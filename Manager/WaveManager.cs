using BounceHeroes.Core;
using System;
using BounceHeroes.Data;
using BounceHeroes.Gameplay;
using BounceHeroes.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 12웨이브의 진행, 처치 수 기반 3택지 노출 조건을 관리합니다.
    /// 실제 스폰 시퀀스 실행은 <see cref="WaveSpawnSequencer"/>(팩토리)에 위임합니다.
    /// </summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveTableData waveTable;
        [SerializeField] private WaveSpawnSequencer sequencer;
        [SerializeField] private float waveStartDelay = 1.2f;
        [SerializeField] private float levelUpBarFillDelay = 0.3f;
        [SerializeField] private int[] experienceRequirements = { 3, 6, 10, 14, 18, 22, 26, 30 };

        private int _waveIndex = -1;
        private int _aliveCount;
        private int _killCount;
        private int _clearedMonsterCount;
        private int _playerLevel = 1;
        private int _currentExperience;
        private bool _levelChoicePending;
        private bool _pendingNextWave;
        private bool _allStepsSpawned;
        private IGameFlow _gameFlow;
        private ISkillService _skills;

        [Inject]
        public void Construct(IGameFlow gameFlow, ISkillService skills)
        {
            _gameFlow = gameFlow;
            _skills = skills;
        }

        private void OnEnable()
        {
            GameplayEvents.SelectionResumed += OnSelectionResumed;
            sequencer.MonsterSpawned += OnMonsterSpawned;
            sequencer.AllStepsSpawned += OnAllStepsSpawned;
        }

        private void OnDisable()
        {
            GameplayEvents.SelectionResumed -= OnSelectionResumed;
            sequencer.MonsterSpawned -= OnMonsterSpawned;
            sequencer.AllStepsSpawned -= OnAllStepsSpawned;
        }

        /// <summary>
        /// 스테이지를 시작하고 첫 웨이브를 진행합니다.
        /// </summary>
        public void StartStage()
        {
            PublishStageProgress();
            StartWaveRoutineAsync(0).Forget();
        }

        private void OnSelectionResumed()
        {
            if (_levelChoicePending)
                ResetExperienceAfterLevelReward();

            if (!_pendingNextWave)
                return;

            _pendingNextWave = false;
            StartWaveRoutineAsync(_waveIndex + 1).Forget();
        }

        private async UniTaskVoid StartWaveRoutineAsync(int index)
        {
            _waveIndex = index;
            _killCount = 0;
            _allStepsSpawned = false;

            WaveData wave = waveTable.GetWave(index);

            GameplayEvents.WaveChanged?.Invoke(index + 1, waveTable.WaveCount);
            GameplayEvents.KillProgressChanged?.Invoke(0, wave.KillRequirement);
            PublishLevelProgress();

            await UniTask.Delay(TimeSpan.FromSeconds(waveStartDelay), cancellationToken: destroyCancellationToken);

            sequencer.Run(wave.Steps);
        }

        private void OnMonsterSpawned(Monster monster)
        {
            monster.Died += OnMonsterDied;
            monster.ReachedBottom += OnMonsterReachedBottom;

            _aliveCount++;
        }

        private void OnAllStepsSpawned()
        {
            _allStepsSpawned = true;

            // 마지막 스텝까지 다 뿌린 시점에 이미 전멸해 있었다면(끝까지 못 따라간 셈), 여기서 클리어 판정한다.
            TryAdvanceIfCleared();
        }

        private void OnMonsterDied(Monster monster)
        {
            monster.Died -= OnMonsterDied;
            monster.ReachedBottom -= OnMonsterReachedBottom;

            _aliveCount--;
            _killCount++;

            _skills.NotifyMonsterKilled(monster);
            AddExperience(1);

            WaveData wave = waveTable.GetWave(_waveIndex);
            GameplayEvents.KillProgressChanged?.Invoke(_killCount, wave.KillRequirement);

            _clearedMonsterCount++;
            PublishStageProgress();

            if (_aliveCount <= 0)
                CombatEvents.WaveLastKill?.Invoke(monster.transform.position);

            TryAdvanceIfCleared();
        }

        private void OnMonsterReachedBottom(Monster monster)
        {
            monster.Died -= OnMonsterDied;
            monster.ReachedBottom -= OnMonsterReachedBottom;

            _aliveCount--;

            _gameFlow.TakePlayerDamage(monster.AttackDamage);

            _clearedMonsterCount++;
            PublishStageProgress();

            TryAdvanceIfCleared();
        }

        /// <summary>
        /// 스테이지 전체 진행률(사라진 몬스터 수 / 전체 몬스터 수)을 HUD 여정 바에 전달합니다.
        /// 죽어서 사라지든 바닥까지 내려가 플레이어를 공격하고 사라지든, 몬스터가 화면에서
        /// 제거되는 시점을 "도착지점까지의 여정이 진행된 것"으로 간주한다.
        /// </summary>
        private void PublishStageProgress()
        {
            GameplayEvents.StageProgressChanged?.Invoke(_clearedMonsterCount, waveTable.TotalMonsterCount);
        }

        /// <summary>
        /// 살아있는 몬스터가 없고 이 웨이브의 모든 스텝을 다 스폰했을 때만 웨이브 클리어로 판정합니다.
        /// 스텝이 남아있는 상태에서 화면의 몬스터만 먼저 전멸하는 경우(강한 광역기 등) 조기 종료를 막는다.
        /// </summary>
        private void TryAdvanceIfCleared()
        {
            if (_aliveCount > 0 || !_allStepsSpawned)
                return;

            bool gameStillActive = _gameFlow.State == GameState.Playing
                || _gameFlow.State == GameState.Selecting;

            if (!gameStillActive)
                return;

            if (_waveIndex + 1 >= waveTable.WaveCount)
                _gameFlow.EndGame(true);
            else
                AdvanceToNextWave();
        }

        /// <summary>
        /// 다음 웨이브로 진행합니다. 3택지 선택 UI가 떠 있는 동안에는 즉시 시작하지 않고,
        /// <see cref="GameplayEvents.SelectionResumed"/>가 발생한 뒤로 미룹니다.
        /// 웨이브 클리어와 3택지 노출 조건이 같은 순간 충족되어 선택 UI와 다음 웨이브 시작이
        /// 동시에 처리되면 정지 상태가 꼬일 수 있어, 이 지연 처리로 그 경합을 없앤다.
        /// </summary>
        private void AdvanceToNextWave()
        {
            if (_gameFlow.State == GameState.Selecting)
                _pendingNextWave = true;
            else
                StartWaveRoutineAsync(_waveIndex + 1).Forget();
        }

        private void TryShowChoices()
        {
            SkillChoice[] choices = _skills.BuildChoices();

            if (choices.Length == 0)
            {
                ResetExperienceAfterLevelReward();
                return;
            }

            _gameFlow.EnterSelection(choices);
        }

        private void AddExperience(int amount)
        {
            if (_levelChoicePending || amount <= 0)
                return;

            _currentExperience += amount;
            int required = GetExperienceRequiredForNextLevel();

            if (_currentExperience < required)
            {
                PublishLevelProgress();
                return;
            }

            _currentExperience = required;
            _levelChoicePending = true;
            PublishLevelProgress();

            // 경험치 바가 100%까지 차오르는 걸 보여준 뒤에 레벨을 올리고 3택지를 띄운다.
            // 레벨을 먼저 올리면 PublishLevelProgress가 "다음 레벨"의 요구량 기준으로 비율을
            // 다시 계산해버려(예: 3/3 → 3/6), 100%로 차는 게 아니라 오히려 뒤로 감기는 것처럼 보인다.
            ShowChoicesAfterBarFillAsync().Forget();
        }

        private async UniTaskVoid ShowChoicesAfterBarFillAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(levelUpBarFillDelay), cancellationToken: destroyCancellationToken);
            _playerLevel++;
            GameplayEvents.LevelChanged?.Invoke(_playerLevel);
            TryShowChoices();
        }

        private void ResetExperienceAfterLevelReward()
        {
            _currentExperience = 0;
            _levelChoicePending = false;
            PublishLevelProgress();
        }

        private void PublishLevelProgress()
        {
            GameplayEvents.LevelProgressChanged?.Invoke(
                _playerLevel,
                _currentExperience,
                GetExperienceRequiredForNextLevel());
        }

        private int GetExperienceRequiredForNextLevel()
        {
            int index = Mathf.Max(0, _playerLevel - 1);

            if (experienceRequirements != null && index < experienceRequirements.Length)
                return Mathf.Max(1, experienceRequirements[index]);

            int lastRequirement = experienceRequirements != null && experienceRequirements.Length > 0
                ? experienceRequirements[experienceRequirements.Length - 1]
                : 30;

            int overflowIndex = experienceRequirements != null
                ? index - experienceRequirements.Length + 1
                : index + 1;

            return lastRequirement + overflowIndex * 4;
        }
    }
}
