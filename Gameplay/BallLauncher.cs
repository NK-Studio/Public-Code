using BounceHeroes.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using BounceHeroes.Data;
using BounceHeroes.Input;
using BounceHeroes.Managers;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using Random = UnityEngine.Random;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 드래그 조준과 볼 발사를 담당합니다.
    /// 게임(Playing)이 시작되면 상시 발사하며, 조준선은 항상 노출됩니다.
    /// 드래그하는 동안에만 발사 방향이 바뀌고, 아래로 스와이프하면 비행 중인 볼을 전체 회수합니다.
    /// </summary>
    public sealed class BallLauncher : MonoBehaviour, IAutoBindable
    {
        [SerializeField, Required] private Ball ballPrefab;
        [SerializeField, Required] private GameBalanceData balance;
        [SerializeField, Required] private TrajectoryPreview trajectory;
        [SerializeField, Required] private Transform launchPoint;
        [SerializeField, Required] private Transform playerVisual;
        [SerializeField] private Transform headVisual;
        [SerializeField] private Transform bodyVisual;
        [SerializeField] private Transform weaponVisual;
        [SerializeField, Required] private Transform collectTarget;
        [SerializeField] private Transform ballSpawnerFxPoint;
        [SerializeField] private float minAimDirectionY = 0.15f;
        [SerializeField] private float recoilStrength = 0.1f;
        [SerializeField] private float headRecoilStrength = 0.04f;
        [SerializeField] private float weaponRecoilStrength = 0.12f;
        [SerializeField] private float bodyRecoilAngle = 7f;
        [SerializeField] private float headRecoilAngle = 5f;
        [SerializeField] private float weaponRecoilAngle = 10f;

        [Header("Recall Swipe")]
        [SerializeField] private float recallSwipeMinDistance = 150f;
        [SerializeField] private float recallSwipeMaxTime = 0.4f;
        [SerializeField, Range(0.5f, 1f)] private float recallSwipeVerticalThreshold = 0.7f;

        private readonly List<Ball> _activeBalls = new List<Ball>();
        private readonly Dictionary<Ball, IObjectPool<Ball>> _ballPools = new Dictionary<Ball, IObjectPool<Ball>>();

        // 현재 비행 중(회수 대기 포함)인 메인 볼 수와, 스킬 구성별 비행 수. 미니볼은 제외한다.
        // 이 둘로 "손에 남은 볼 수 = VolleySize - 비행 중"과 각 볼이 되쏠 스킬 종류를 판단한다.
        private readonly Dictionary<(ActiveSkillData Skill, int Level), int> _inFlightBySpec =
            new Dictionary<(ActiveSkillData, int), int>();
        private readonly Dictionary<(ActiveSkillData Skill, int Level), int> _desiredSpecScratch =
            new Dictionary<(ActiveSkillData, int), int>();
        private int _ballsInFlight;
        private Transform _poolRoot;
        private Camera _camera;
        private CancellationTokenSource _fireCts;
        private SwipeDetector _recallSwipe;
        private bool _isAiming;
        private bool _wasPressed;
        private Vector2 _aimDirection = Vector2.up;
        private Vector3 _playerBaseLocalPosition;
        private Vector3 _headBaseLocalPosition;
        private Vector3 _weaponBaseLocalPosition;
        private MotionHandle _playerRecoilMotion;
        private MotionHandle _weaponRecoilMotion;
        private MotionHandle _bodyRecoilMotion;
        private MotionHandle _headRecoilMotion;
        private IFXService _fx;
        private IGameFlow _gameFlow;
        private ISkillService _skills;
        private ICombatService _combat;

        [Inject]
        public void Construct(IFXService fx, IGameFlow gameFlow, ISkillService skills, ICombatService combat)
        {
            _fx = fx;
            _gameFlow = gameFlow;
            _skills = skills;
            _combat = combat;

            WarmUpHitFx();
        }

        private const int DefaultBallHitFxPrewarmCount = 10;
        private const int SkillBallHitFxPrewarmCount = 1;

        /// <summary>
        /// 모든 볼 종류(기본 볼 + 3택지 풀의 스킬 볼)가 각자 들고 있는 타격 FX 풀을 미리 채워둔다.
        /// 지연 생성에 맡기면 그 볼의 첫 타격 때 Instantiate로 렉이 튄다.
        /// 기본 볼은 가장 자주 재생되므로 넉넉히, 아직 안 뽑은 스킬 볼은 1개씩만 준비한다.
        /// </summary>
        private void WarmUpHitFx()
        {
            if (_fx == null)
                return;

            HashSet<GameObject> warmedPrefabs = new HashSet<GameObject>();

            WarmUpBallHitFx(ballPrefab, DefaultBallHitFxPrewarmCount, warmedPrefabs);

            if (_skills == null)
                return;

            foreach (Ball ball in _skills.GetAllBallPrefabs())
                WarmUpBallHitFx(ball, SkillBallHitFxPrewarmCount, warmedPrefabs);
        }

        private void WarmUpBallHitFx(Ball ball, int count, HashSet<GameObject> warmedPrefabs)
        {
            BallVisualController visual = ball != null ? ball.VisualController : null;
            GameObject hitFxPrefab = visual != null ? visual.HitFxPrefab : null;

            if (hitFxPrefab == null || !warmedPrefabs.Add(hitFxPrefab))
                return;

            Vector3 offscreen = new Vector3(0f, -1000f, 0f);

            // lifetime이 0 이하면 같은 프레임에 곧바로 풀로 반환되어, 반복 호출이 매번 같은
            // 인스턴스 하나만 재사용하게 된다. 별개의 인스턴스를 만들려면 반환이 다음 프레임
            // 이후로 미뤄지도록 항상 0보다 큰 수명을 준다.
            float lifetime = visual.HitFxLifetime > 0f ? visual.HitFxLifetime : 0.05f;

            for (int i = 0; i < count; i++)
                _fx.Play(hitFxPrefab, offscreen, Quaternion.identity, 1f, lifetime);
        }

        /// <summary>플레이어 캐릭터 비주얼의 트랜스폼입니다. 피격 이펙트 등 위치 기준으로 씁니다.</summary>
        public Transform PlayerVisual => playerVisual != null ? playerVisual : transform;

        /// <summary>회수된 볼이 최종적으로 날아가 사라질 목표 지점입니다.</summary>
        public Transform CollectTarget => collectTarget;

        private void Awake()
        {
            _camera = Camera.main;
            CacheRecoilPose();

            _recallSwipe = new SwipeDetector(recallSwipeMinDistance, recallSwipeMaxTime, recallSwipeVerticalThreshold);
        }

        private void Update()
        {
            if (_gameFlow == null || _gameFlow.State != GameState.Playing)
            {
                StopFiring();
                trajectory.Hide();
                _isAiming = false;
                _wasPressed = false;
                return;
            }

            // 게임이 시작(Playing)되면 손 조작과 무관하게 볼을 계속 발사한다.
            StartFiring();

            // 조준선은 상시 노출되며, 마지막으로 정해진 발사 방향을 가리킨다.
            trajectory.Show(launchPoint.position, _aimDirection);

            if (!PointerInput.TryGetCurrent(out Vector2 pointerPosition, out bool isPressed))
            {
                _wasPressed = false;
                return;
            }

            // wasPressedThisFrame/wasReleasedThisFrame는 그 전환이 일어난 정확히 한 프레임에만
            // 참이 되어, 프레임이 드물게라도 밀리면 시작/종료 시점을 놓칠 수 있다.
            // 이전 프레임과 비교해 직접 눌림 전환을 계산하면 이런 프레임 유실에 영향받지 않는다.
            bool pressedThisFrame = isPressed && !_wasPressed;
            bool releasedThisFrame = !isPressed && _wasPressed;
            _wasPressed = isPressed;

            if (pressedThisFrame)
            {
                _isAiming = true;
                _recallSwipe.BeginTrack(pointerPosition);
            }
            else if (releasedThisFrame)
            {
                _isAiming = false;

                // 단순 터치가 아니라 아래로 스와이프해야 회수되도록 한다. 조준 드래그는 위쪽을
                // 향하므로(minAimDirectionY) 아래 스와이프와 자연스럽게 구분된다.
                if (_recallSwipe.EndTrack(pointerPosition) == SwipeDirection.Down)
                    RecallAll();
            }

            // 드래그하는 동안에만 발사 방향을 갱신하고, 손을 떼면 마지막 방향을 유지한다.
            if (isPressed && _isAiming)
                UpdateAim(pointerPosition);
        }

        private void UpdateAim(Vector2 pointerPosition)
        {
            Vector2 world = _camera.ScreenToWorldPoint(pointerPosition);
            Vector2 direction = world - (Vector2)launchPoint.position;

            if (direction.sqrMagnitude > 0.04f && direction.normalized.y >= minAimDirectionY)
                _aimDirection = direction.normalized;
        }

        /// <summary>
        /// 회수된 볼을 발사기 관리 목록에서 제거합니다.
        /// </summary>
        /// <param name="ball">회수된 볼</param>
        public void NotifyBallReturned(Ball ball)
        {
            _activeBalls.Remove(ball);

            // 되돌아온 메인 볼은 손으로 돌아온 것으로 보고 비행 카운터를 줄인다.
            // 발사 루프가 곧바로 다음 발사에 이 볼을 다시 사용한다. 미니볼은 인벤토리에 포함하지 않는다.
            if (ball == null || ball.IsMini)
                return;

            _ballsInFlight = Mathf.Max(0, _ballsInFlight - 1);
            ReleaseSpec(ball.Skill, ball.Level);
        }

        /// <summary>
        /// 소형 특수 볼을 생성합니다. 프리팹·스프라이트·속도 등 소형 볼의 데이터는 호출자(예: 클러스터 볼 스킬)가 소유하며,
        /// 발사기는 풀링과 발사 절차만 담당합니다.
        /// </summary>
        /// <param name="prefab">소형 볼 프리팹. null이면 기본 볼로 대체합니다.</param>
        /// <param name="sprites">무작위로 적용할 소형 볼 스프라이트 후보. 비어 있으면 프리팹 기본 스프라이트를 씁니다.</param>
        /// <param name="position">생성 위치</param>
        /// <param name="damage">소형 볼의 충돌 데미지</param>
        /// <param name="speed">이동 속도</param>
        /// <param name="flightTime">자동 회수까지의 시간(초)</param>
        public void SpawnMiniBall(Ball prefab, Sprite[] sprites, Vector2 position, int damage, float speed, float flightTime)
        {
            float angle = Random.Range(35f, 145f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Ball ball = GetBallPool(prefab != null ? prefab : ballPrefab).Get();
            ball.SetFXService(_fx);
            ball.SetCombatService(_combat);
            ball.SetAsMini();

            if (sprites != null && sprites.Length > 0)
                ball.SetSprite(sprites[Random.Range(0, sprites.Length)]);

            ball.Fire(this, position, direction, null, 0, damage, speed, flightTime);

            _activeBalls.Add(ball);
        }

        /// <summary>
        /// 비행 중인 모든 볼을 강제 회수합니다. 상시 발사는 계속 이어집니다.
        /// </summary>
        public void RecallAll()
        {
            foreach (Ball ball in _activeBalls)
            {
                if (ball != null)
                    ball.ForceRecall();
            }
        }

        /// <summary>Playing 상태에 진입하면 상시 발사 루프를 시작합니다. 중복 호출은 무시됩니다.</summary>
        private void StartFiring()
        {
            if (_fireCts != null)
                return;

            _fireCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            FireLoopAsync(_fireCts.Token).Forget();
        }

        /// <summary>상시 발사 루프를 중단합니다.</summary>
        private void StopFiring()
        {
            if (_fireCts == null)
                return;

            _fireCts.Cancel();
            _fireCts.Dispose();
            _fireCts = null;
        }

        /// <summary>
        /// 플레이어는 VolleySize개의 볼을 인벤토리로 들고 있습니다. 손에 볼이 있는 동안(비행 중 볼 수 &lt; VolleySize)
        /// FireInterval 간격으로 계속 발사하고, 흩어졌다 되돌아온 볼은 다른 볼을 기다리지 않고 곧바로 다시 발사됩니다.
        /// 각 발사에는 스킬 로드아웃 대비 아직 비행 중이지 않은 종류를 우선 채워, 구성(특수/일반 볼 개수)을 유지합니다.
        /// </summary>
        private async UniTaskVoid FireLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<(ActiveSkillData Skill, int Level)> specials = _skills.GetActiveLoadout();
                int capacity = Mathf.Max(balance.VolleySize, specials.Count);

                // 손에 남은 볼이 없으면(모두 비행 중) 한 프레임 쉬며 볼이 되돌아오길 기다린다.
                if (_ballsInFlight >= capacity)
                {
                    await UniTask.Yield(token);
                    continue;
                }

                (ActiveSkillData Skill, int Level) spec = PickNextSpec(specials, capacity);
                FireBall(spec.Skill, spec.Level);

                await UniTask.Delay(TimeSpan.FromSeconds(balance.FireInterval), cancellationToken: token);
            }
        }

        /// <summary>
        /// 로드아웃 구성 대비 아직 비행 중이지 않은(부족한) 스킬 종류를 하나 골라 반환합니다.
        /// 손에 볼이 있는 상태(_ballsInFlight &lt; capacity)에서 호출되므로 항상 부족분이 존재합니다.
        /// </summary>
        private (ActiveSkillData Skill, int Level) PickNextSpec(
            List<(ActiveSkillData Skill, int Level)> specials, int capacity)
        {
            _desiredSpecScratch.Clear();

            for (int i = 0; i < capacity; i++)
            {
                (ActiveSkillData Skill, int Level) key = i < specials.Count
                    ? (specials[i].Skill, specials[i].Level)
                    : (null, 0);

                _desiredSpecScratch.TryGetValue(key, out int desired);
                _desiredSpecScratch[key] = desired + 1;
            }

            foreach (KeyValuePair<(ActiveSkillData Skill, int Level), int> entry in _desiredSpecScratch)
            {
                _inFlightBySpec.TryGetValue(entry.Key, out int inFlight);

                if (inFlight < entry.Value)
                    return entry.Key;
            }

            // 이론상 도달하지 않지만(부족분이 항상 있음) 안전하게 일반 볼로 대체한다.
            return (null, 0);
        }

        private void FireBall(ActiveSkillData skill, int level)
        {
            int damage = skill != null ? skill.GetBallDamage(level) : balance.NormalBallDamage;

            Ball ball = GetBallPool(ResolveBallPrefab(skill)).Get();
            ball.SetFXService(_fx);
            ball.SetCombatService(_combat);
            ball.Fire(this, launchPoint.position, _aimDirection, skill, level, damage,
                balance.BallSpeed, balance.MaxFlightTime);

            _activeBalls.Add(ball);
            _ballsInFlight++;
            AcquireSpec(skill, level);

            PlayBallSpawnerFx();
            PlayRecoil();
            CombatEvents.BallFired?.Invoke(launchPoint.position, _aimDirection);
        }

        private void AcquireSpec(ActiveSkillData skill, int level)
        {
            (ActiveSkillData, int) key = (skill, level);
            _inFlightBySpec.TryGetValue(key, out int count);
            _inFlightBySpec[key] = count + 1;
        }

        private void ReleaseSpec(ActiveSkillData skill, int level)
        {
            (ActiveSkillData, int) key = (skill, level);

            if (!_inFlightBySpec.TryGetValue(key, out int count))
                return;

            if (count <= 1)
                _inFlightBySpec.Remove(key);
            else
                _inFlightBySpec[key] = count - 1;
        }

        private Ball ResolveBallPrefab(ActiveSkillData skill)
        {
            if (skill != null && skill.BallPrefab != null)
                return skill.BallPrefab;

            return ballPrefab;
        }

        /// <summary>
        /// 프리팹별 볼 오브젝트 풀을 지연 생성하여 반환합니다. 스킬마다 프리팹이 달라 프리팹 키로 관리합니다.
        /// </summary>
        private IObjectPool<Ball> GetBallPool(Ball prefab)
        {
            if (_ballPools.TryGetValue(prefab, out IObjectPool<Ball> existing))
                return existing;

            IObjectPool<Ball> pool = null;
            pool = new ObjectPool<Ball>(
                createFunc: () =>
                {
                    Ball ball = Instantiate(prefab, PoolRoot);
                    ball.BindPool(() => pool.Release(ball));
                    return ball;
                },
                actionOnGet: ball =>
                {
                    ball.gameObject.SetActive(true);
                    ball.ResetState();
                },
                actionOnRelease: ball =>
                {
                    ball.transform.SetParent(PoolRoot, false);
                    ball.gameObject.SetActive(false);
                },
                actionOnDestroy: ball =>
                {
                    if (ball != null)
                        Destroy(ball.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 128);

            _ballPools[prefab] = pool;
            return pool;
        }

        /// <summary>계층창에서 볼 풀 인스턴스를 한 곳에 모아두기 위한 부모입니다.</summary>
        private Transform PoolRoot => _poolRoot ??= new GameObject("[Ball Pool]").transform;

        private void PlayRecoil()
        {
            Vector3 rootRecoil = ToParentLocalOffset(playerVisual, -_aimDirection * recoilStrength * 0.35f);

            // 발사 방향의 반대로 살짝 밀렸다가 돌아오는 몸통 반동.
            // PlayerAimPose가 localScale로 좌우 반전을 제어하므로 스케일은 건드리지 않는다.
            if (playerVisual != null)
            {
                _playerRecoilMotion.TryCancel();
                playerVisual.localPosition = _playerBaseLocalPosition;

                _playerRecoilMotion = LSequence.Create()
                    .Append(LMotion.Create(_playerBaseLocalPosition, _playerBaseLocalPosition + rootRecoil, 0.045f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalPosition(playerVisual))
                    .Append(LMotion.Create(_playerBaseLocalPosition + rootRecoil, _playerBaseLocalPosition, 0.14f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalPosition(playerVisual))
                    .Run();
            }

            // 무기가 발사 방향 반대로 튕겨나가는 킥백 연출.
            if (weaponVisual != null)
            {
                _weaponRecoilMotion.TryComplete();
                weaponVisual.localPosition = _weaponBaseLocalPosition;

                Vector3 weaponRecoil = ToParentLocalOffset(weaponVisual, -_aimDirection * weaponRecoilStrength);
                float weaponBaseZ = NormalizeAngle(weaponVisual.localEulerAngles.z);
                float turnSign = ResolveRecoilTurnSign();
                float weaponKickZ = weaponBaseZ + turnSign * weaponRecoilAngle;
                Vector3 weaponBaseEuler = new Vector3(0f, 0f, weaponBaseZ);
                Vector3 weaponKickEuler = new Vector3(0f, 0f, weaponKickZ);

                _weaponRecoilMotion = LSequence.Create()
                    .Append(LMotion.Create(_weaponBaseLocalPosition, _weaponBaseLocalPosition + weaponRecoil, 0.035f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalPosition(weaponVisual))
                    .Join(LMotion.Create(weaponBaseEuler, weaponKickEuler, 0.035f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalEulerAngles(weaponVisual))
                    .Append(LMotion.Create(_weaponBaseLocalPosition + weaponRecoil, _weaponBaseLocalPosition, 0.12f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalPosition(weaponVisual))
                    .Join(LMotion.Create(weaponKickEuler, weaponBaseEuler, 0.12f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalEulerAngles(weaponVisual))
                    .Run();
            }

            // 하단 피벗을 기준으로 몸통이 반대 방향으로 젖혀졌다 돌아오는 반동.
            if (bodyVisual != null)
            {
                _bodyRecoilMotion.TryComplete();
                float bodyBaseZ = NormalizeAngle(bodyVisual.localEulerAngles.z);
                float bodyKickZ = bodyBaseZ + ResolveRecoilTurnSign() * bodyRecoilAngle;
                Vector3 bodyBaseEuler = new Vector3(0f, 0f, bodyBaseZ);
                Vector3 bodyKickEuler = new Vector3(0f, 0f, bodyKickZ);

                _bodyRecoilMotion = LSequence.Create()
                    .Append(LMotion.Create(bodyBaseEuler, bodyKickEuler, 0.06f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalEulerAngles(bodyVisual))
                    .Append(LMotion.Create(bodyKickEuler, bodyBaseEuler, 0.14f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalEulerAngles(bodyVisual))
                    .Run();
            }

            if (headVisual != null)
            {
                _headRecoilMotion.TryComplete();
                headVisual.localPosition = _headBaseLocalPosition;

                Vector3 headRecoil = ToParentLocalOffset(headVisual, -_aimDirection * headRecoilStrength);
                float headBaseZ = NormalizeAngle(headVisual.localEulerAngles.z);
                float headKickZ = headBaseZ + ResolveRecoilTurnSign() * headRecoilAngle;
                Vector3 headBaseEuler = new Vector3(0f, 0f, headBaseZ);
                Vector3 headKickEuler = new Vector3(0f, 0f, headKickZ);

                _headRecoilMotion = LSequence.Create()
                    .Append(LMotion.Create(_headBaseLocalPosition, _headBaseLocalPosition + headRecoil, 0.05f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalPosition(headVisual))
                    .Join(LMotion.Create(headBaseEuler, headKickEuler, 0.05f)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalEulerAngles(headVisual))
                    .Append(LMotion.Create(_headBaseLocalPosition + headRecoil, _headBaseLocalPosition, 0.13f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalPosition(headVisual))
                    .Join(LMotion.Create(headKickEuler, headBaseEuler, 0.13f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalEulerAngles(headVisual))
                    .Run();
            }
        }

        /// <summary>
        /// playerVisual 하위의 시각 요소(Head/Body/Weapon/Center)와 FX 포인트를 찾아 비어 있는 참조만 채웁니다.
        /// 에디터 "Auto Bind"에서만 호출되며, 런타임에 스스로 채우지 않습니다.
        /// </summary>
        public void AutoBind()
        {
            if (playerVisual == null)
                return;

            if (headVisual == null)
                headVisual = playerVisual.Find("Head");

            if (bodyVisual == null)
                bodyVisual = playerVisual.Find("Body");

            if (weaponVisual == null)
                weaponVisual = playerVisual.Find("Weapon");

            if (collectTarget == null)
                collectTarget = playerVisual.Find("Center");

            if (ballSpawnerFxPoint == null)
                ballSpawnerFxPoint = FindBallSpawnerFxPoint();
        }

        private Transform FindBallSpawnerFxPoint()
        {
            Transform fxPoint = playerVisual.Find("Weapon/FX Point");

            if (fxPoint != null)
                return fxPoint;

            fxPoint = playerVisual.Find("Weaphone/FX Point");

            if (fxPoint != null)
                return fxPoint;

            return FindChildByName(playerVisual, "FX Point");
        }

        private void PlayBallSpawnerFx()
        {
            if (ballSpawnerFxPoint == null)
                return;

            _fx.Play(FXId.BallSpawner, ballSpawnerFxPoint.position, ballSpawnerFxPoint.rotation, 1f);
        }

        private void CacheRecoilPose()
        {
            if (playerVisual != null)
                _playerBaseLocalPosition = playerVisual.localPosition;

            if (headVisual != null)
                _headBaseLocalPosition = headVisual.localPosition;

            if (weaponVisual != null)
                _weaponBaseLocalPosition = weaponVisual.localPosition;
        }

        private void OnDisable()
        {
            ResetRecoilPose();
        }

        private void ResetRecoilPose()
        {
            if (playerVisual != null)
            {
                _playerRecoilMotion.TryComplete();
                playerVisual.localPosition = _playerBaseLocalPosition;
            }

            if (headVisual != null)
            {
                _headRecoilMotion.TryComplete();
                headVisual.localPosition = _headBaseLocalPosition;
            }

            if (bodyVisual != null)
                _bodyRecoilMotion.TryComplete();

            if (weaponVisual != null)
            {
                _weaponRecoilMotion.TryComplete();
                weaponVisual.localPosition = _weaponBaseLocalPosition;
            }
        }

        private static Vector3 ToParentLocalOffset(Transform target, Vector2 worldOffset)
        {
            if (target == null || target.parent == null)
                return worldOffset;

            return target.parent.InverseTransformVector(worldOffset);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName)
                    return child;

                Transform nested = FindChildByName(child, childName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private float ResolveRecoilTurnSign()
        {
            if (playerVisual == null)
                return _aimDirection.x >= 0f ? -1f : 1f;

            Vector3 localAim = playerVisual.InverseTransformDirection(_aimDirection);

            if (Mathf.Abs(localAim.x) < 0.02f)
                return 0f;

            return -Mathf.Sign(localAim.x);
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
    }
}
