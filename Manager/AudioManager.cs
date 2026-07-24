using BounceHeroes.Core;
using BounceHeroes.Data;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 정적 이벤트 허브를 구독해 "언제 어떤 사운드를 울릴지"를 결정하고 <see cref="IAudioService"/>에 위임합니다.
    /// 실제 재생/볼륨 제어는 서비스가 담당하며, 이 매니저는 배선만 합니다(=JuiceManager와 동일한 역할 분담).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        /// <summary>플레이어 사망 시 현재 BGM(event:/BGM/Main)을 사망 상태로 전환하는 파라미터 이름입니다.</summary>
        private const string BgmDeadParameter = "Dead";

        private IAudioService _audio;

        /// <summary>
        /// 같은 타격에서 <see cref="CombatEvents.SkillHitLanded"/>가 먼저 재생한 사운드가 있으면,
        /// 뒤이어 오는 <see cref="CombatEvents.MonsterHitLanded"/>의 기본 타격음을 대체하도록 표시합니다.
        /// (CombatService.ResolveHit이 항상 SkillHitLanded → monster.TakeDamage(MonsterHitLanded) 순서로 호출하므로 안전합니다.)
        /// </summary>
        private bool _skillHitSfxPlayed;

        [Inject]
        public void Construct(IAudioService audio)
        {
            _audio = audio;
            _audio.RestartBgm(AudioId.BgmGameplay);
        }

        private void OnEnable()
        {
            _skillHitSfxPlayed = false;

            CombatEvents.BallFired += OnBallFired;
            CombatEvents.MonsterHitLanded += OnMonsterHitLanded;
            CombatEvents.MonsterKilled += OnMonsterKilled;
            CombatEvents.WaveLastKill += OnWaveLastKill;
            CombatEvents.ExplosionFired += OnExplosionFired;
            CombatEvents.RowLaserFired += OnLaserHit;
            CombatEvents.PlayerHit += OnPlayerHit;
            CombatEvents.SkillHitLanded += OnSkillHitLanded;

            GameplayEvents.WaveChanged += OnWaveChanged;
            GameplayEvents.LevelChanged += OnLevelChanged;
            GameplayEvents.SkillChoicesReady += OnSkillChoicesReady;
            GameplayEvents.GameEnded += OnGameEnded;

            GameUIEvents.SkillChoiceSelected += OnSkillChoiceSelected;
            GameUIEvents.PauseRequested += OnPauseRequested;
            GameUIEvents.PauseCloseRequested += OnPauseCloseRequested;
            GameUIEvents.BgmEnabledChanged += OnBgmEnabledChanged;
            GameUIEvents.SfxEnabledChanged += OnSfxEnabledChanged;
        }

        private void OnDisable()
        {
            CombatEvents.BallFired -= OnBallFired;
            CombatEvents.MonsterHitLanded -= OnMonsterHitLanded;
            CombatEvents.MonsterKilled -= OnMonsterKilled;
            CombatEvents.WaveLastKill -= OnWaveLastKill;
            CombatEvents.ExplosionFired -= OnExplosionFired;
            CombatEvents.RowLaserFired -= OnLaserHit;
            CombatEvents.PlayerHit -= OnPlayerHit;
            CombatEvents.SkillHitLanded -= OnSkillHitLanded;

            GameplayEvents.WaveChanged -= OnWaveChanged;
            GameplayEvents.LevelChanged -= OnLevelChanged;
            GameplayEvents.SkillChoicesReady -= OnSkillChoicesReady;
            GameplayEvents.GameEnded -= OnGameEnded;

            GameUIEvents.SkillChoiceSelected -= OnSkillChoiceSelected;
            GameUIEvents.PauseRequested -= OnPauseRequested;
            GameUIEvents.PauseCloseRequested -= OnPauseCloseRequested;
            GameUIEvents.BgmEnabledChanged -= OnBgmEnabledChanged;
            GameUIEvents.SfxEnabledChanged -= OnSfxEnabledChanged;
        }

        // --- 전투 SFX ---
        private void OnBallFired(Vector3 position, Vector2 direction) => _audio.PlaySfx(AudioId.BallFire, position);

        private void OnMonsterHitLanded(Vector3 position, bool isCrit)
        {
            // 같은 타격에서 스킬 전용 사운드가 이미 재생됐다면 기본 타격음은 재생하지 않는다.
            if (_skillHitSfxPlayed)
            {
                _skillHitSfxPlayed = false;
                return;
            }

            _audio.PlaySfx(isCrit ? AudioId.MonsterCrit : AudioId.MonsterHit, position);
        }

        private void OnSkillHitLanded(AudioId id, Vector3 position)
        {
            _skillHitSfxPlayed = true;
            _audio.PlaySfx(id, position);
        }

        private void OnMonsterKilled(Vector3 position) => _audio.PlaySfx(AudioId.MonsterKill, position);

        private void OnWaveLastKill(Vector3 position) => _audio.PlaySfx(AudioId.WaveClear, position);

        private void OnExplosionFired(Vector3 position, float radius) => _audio.PlaySfx(AudioId.Explosion, position);

        private void OnLaserHit(float worldY) => _audio.PlaySfx(AudioId.SkillLaserShow, new Vector3(0f, worldY, 0f));
        
        private void OnPlayerHit(Vector3 position) => _audio.PlaySfx(AudioId.PlayerHit, position);

        // --- 게임 흐름 / BGM ---
        private void OnWaveChanged(int current, int total) => _audio.PlayBgm(AudioId.BgmGameplay);

        private void OnLevelChanged(int level) => _audio.PlaySfx(AudioId.LevelUp);

        private void OnSkillChoicesReady(SkillChoice[] choices) => _audio.PlaySfx(AudioId.UIOpenSelectSkillMenu);

        private void OnGameEnded(bool won)
        {
            if (won)
            {
                // 승리: 재생 중이던 Main BGM을 정지한다.
                _audio.StopBgm();
                return;
            }

            // 플레이어 사망: 별도 이벤트로 교체하지 않고, 재생 중인 Main BGM의 "Dead" 파라미터를 1로 설정한다.
            _audio.SetBgmParameter(BgmDeadParameter, 1f);
        }

        // --- UI SFX ---
        private void OnSkillChoiceSelected(int index) => _audio.PlaySfx(AudioId.UISelectSkillChoice);

        private void OnPauseRequested()
        {
            _audio.PlaySfx(AudioId.UiOpenPauseMenu);
            _audio.PlaySnapshot(AudioId.SnapshotPause);
        }

        private void OnPauseCloseRequested()
        {
            _audio.PlaySfx(AudioId.UiClosePauseMenu);
            _audio.StopSnapshot();
        }

        // --- 설정 토글 → VCA ---
        private void OnBgmEnabledChanged(bool enabled) => _audio.SetBgmEnabled(enabled);

        private void OnSfxEnabledChanged(bool enabled) => _audio.SetSfxEnabled(enabled);
    }
}
