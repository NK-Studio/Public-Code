using System;
using System.Collections.Generic;
using System.Threading;
using BounceHeroes.Core;
using BounceHeroes.Data;
using BounceHeroes.Gameplay;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 웨이브 디자이너에서 저작한 <see cref="SpawnStep"/> 시퀀스를 순서대로 소비하는 스폰 팩토리입니다.
    /// 한 스텝의 몬스터를 전부 배치한 뒤, 그 몬스터 전원이 기준 행을 통과하거나
    /// 죽음/바닥도달로 제거될 때까지 기다린 다음에야 다음 스텝을 스폰합니다.
    /// </summary>
    public sealed class WaveSpawnSequencer : MonoBehaviour
    {
        [SerializeField] private GridField grid;
        [SerializeField] private Monster monsterPrefab;
        [SerializeField] private GameBalanceData balance;
        [SerializeField] private float placementStagger = 0.06f;
        [SerializeField] private int triggerRow = 1;

        private readonly List<Monster> _pendingGate = new List<Monster>();
        private CancellationTokenSource _cts;
        private IFXService _fx;
        private BallLauncher _launcher;
        private IObjectPool<Monster> _monsterPool;
        private Transform _poolRoot;

        [Inject]
        public void Construct(IFXService fx, BallLauncher launcher)
        {
            _fx = fx;
            _launcher = launcher;
        }

        /// <summary>몬스터 오브젝트 풀입니다. 단일 프리팹이라 하나의 풀로 관리합니다.</summary>
        private IObjectPool<Monster> MonsterPool =>
            _monsterPool ??= new ObjectPool<Monster>(
                createFunc: () =>
                {
                    Monster monster = Instantiate(monsterPrefab, PoolRoot);
                    monster.BindPool(() => _monsterPool.Release(monster));
                    monster.gameObject.SetActive(false);
                    return monster;
                },
                actionOnGet: monster => monster.gameObject.SetActive(true),
                actionOnRelease: monster =>
                {
                    monster.transform.SetParent(PoolRoot, false);
                    monster.gameObject.SetActive(false);
                },
                actionOnDestroy: monster =>
                {
                    if (monster != null)
                        Destroy(monster.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 32,
                maxSize: 128);

        /// <summary>계층창에서 몬스터 풀 인스턴스를 한 곳에 모아두기 위한 부모입니다.</summary>
        private Transform PoolRoot => _poolRoot ??= new GameObject("[Monster Pool]").transform;

        /// <summary>몬스터 한 마리가 스폰될 때마다 발생합니다. 처치/바닥도달 이벤트 구독에 사용합니다.</summary>
        public event Action<Monster> MonsterSpawned;

        /// <summary>시퀀스의 마지막 스텝까지 모두 스폰을 마쳤을 때 발생합니다.</summary>
        public event Action AllStepsSpawned;

        private void Start()
        {
            // 몬스터 오브젝트 풀을 초기화하고 미리 20마리를 생성하여 풀에 적재합니다.
            PreWarmPool(20);
        }
        
        private void PreWarmPool(int count)
        {
            var pool = MonsterPool;
            var tempInstances = new Monster[count];
            for (int i = 0; i < count; i++) 
                tempInstances[i] = pool.Get();
            
            for (int i = 0; i < count; i++) 
                pool.Release(tempInstances[i]);
        }
        /// <summary>
        /// 스텝 시퀀스를 처음부터 실행합니다. 기존에 실행 중이던 시퀀스가 있으면 중단하고 새로 시작합니다.
        /// </summary>
        /// <param name="steps">순서대로 스폰할 스텝 목록</param>
        public void Run(SpawnStep[] steps)
        {
            Stop();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunStepsAsync(steps, _cts.Token).Forget();
        }

        /// <summary>
        /// 진행 중인 스폰 시퀀스를 중단합니다.
        /// </summary>
        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _pendingGate.Clear();
        }

        private async UniTaskVoid RunStepsAsync(SpawnStep[] steps, CancellationToken token)
        {
            foreach (SpawnStep step in steps)
            {
                SpawnStepPlacements(step);
                await WaitUntilGateClearAsync(token);
            }

            // 정상 완료: 실행 중 표시(CTS)를 정리한 뒤 완료를 알린다(코루틴 버전의 _routine = null과 동일 취지).
            _cts?.Dispose();
            _cts = null;
            AllStepsSpawned?.Invoke();
        }

        /// <summary>
        /// 한 스텝의 몬스터를 전부 같은 프레임에 생성합니다. 등장 연출만 몬스터마다
        /// <see cref="placementStagger"/>만큼 순서대로 지연시켜, 실제로는 동시 생성이지만
        /// 순차적으로 나타나는 것처럼 보이게 한다.
        /// </summary>
        private void SpawnStepPlacements(SpawnStep step)
        {
            _pendingGate.Clear();

            int index = 0;

            foreach (PlacedMonster placement in step.Placements)
            {
                if (placement.Monster == null)
                    continue;

                _pendingGate.Add(SpawnPlacement(placement, index * placementStagger));
                index++;
            }
        }

        private Monster SpawnPlacement(PlacedMonster placement, float spawnVisualDelay)
        {
            int hp = placement.HpOverride > 0
                ? placement.HpOverride
                : Mathf.RoundToInt(placement.Monster.BaseHp * balance.MonsterHpMultiplier);

            PlacedMonster.GetRotatedFootprint(placement.Monster, placement.Rotation, out int footprintWidth, out int footprintHeight);
            Vector3 spawnPosition = grid.FootprintToWorldCenter(
                placement.Row,
                placement.Col,
                footprintWidth,
                footprintHeight);

            Monster monster = MonsterPool.Get();
            monster.SetFXService(_fx);
            monster.SetAttackTarget(_launcher != null ? _launcher.CollectTarget : null);
            monster.Initialize(placement.Monster, hp, grid, spawnPosition, balance.DescendSpeed, placement.Rotation);

            grid.Register(monster);
            monster.PlaySpawn(spawnVisualDelay);

            MonsterSpawned?.Invoke(monster);
            return monster;
        }

        private async UniTask WaitUntilGateClearAsync(CancellationToken token)
        {
            float triggerY = grid.CellToWorld(triggerRow, 0).y;

            while (true)
            {
                _pendingGate.RemoveAll(m => m == null || m.IsDead || m.transform.position.y <= triggerY);

                if (_pendingGate.Count == 0)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}
