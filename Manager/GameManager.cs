using System;
using BounceHeroes.Core;
using BounceHeroes.Data;
using BounceHeroes.Gameplay;
using BounceHeroes.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 게임 전체 상태(플레이/선택/승패)와 플레이어 체력을 관리하는 중앙 매니저입니다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour, IGameFlow
    {
        [SerializeField] private GameBalanceData balance;
        [SerializeField] private GridField grid;
        [SerializeField] private BallLauncher launcher;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private PlayerDeathVisual deathVisual;
        [SerializeField] private float resultPopupDelay = 1.5f;
        [SerializeField] private float deadResultPopupDelay = 0.5f;

        private int _playerHp;

        /// <summary>현재 게임 상태입니다.</summary>
        public GameState State { get; private set; } = GameState.Ready;

        /// <summary>게임 밸런스 설정입니다.</summary>
        public GameBalanceData Balance => balance;

        /// <summary>격자 필드 참조입니다.</summary>
        public GridField Grid => grid;

        /// <summary>볼 발사기 참조입니다.</summary>
        public BallLauncher Launcher => launcher;

        private void Awake()
        {
          //  Application.targetFrameRate = 60;

            // 씬이 재시작되어도 이전 플레이의 정지 상태가 남지 않도록 여기서 항상 초기화한다.
            GameTime.Bind(this);

            int ballLayer = LayerMask.NameToLayer("Ball");

            if (ballLayer >= 0)
                Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true);
        }

        private void OnEnable()
        {
            GameUIEvents.GameStartRequested += BeginPlay;
        }

        private void OnDisable()
        {
            GameUIEvents.GameStartRequested -= BeginPlay;
        }

        private void Start()
        {
            // 인트로 트랜지션이 걷히는 동안 HUD가 비어 보이지 않도록 체력만 미리 표시하고,
            // 실제 웨이브 진행은 UI가 GameStartRequested를 알릴 때까지 Ready 상태로 대기한다.
            _playerHp = balance.PlayerMaxHp;
            GameplayEvents.PlayerHpChanged?.Invoke(_playerHp, balance.PlayerMaxHp);
        }

        /// <summary>
        /// 인트로 트랜지션(페이드 아웃)과 대기 시간이 끝난 뒤 실제 게임 플레이(웨이브 진행)를 시작합니다.
        /// </summary>
        private void BeginPlay()
        {
            if (State != GameState.Ready)
                return;

            State = GameState.Playing;
            waveManager.StartStage();
        }

        /// <summary>
        /// 플레이어에게 피해를 입힙니다. 체력이 0 이하가 되면 패배 처리됩니다.
        /// </summary>
        /// <param name="amount">피해량</param>
        public void TakePlayerDamage(int amount)
        {
            if (State != GameState.Playing)
                return;

            _playerHp = Mathf.Max(0, _playerHp - amount);
            GameplayEvents.PlayerHpChanged?.Invoke(_playerHp, balance.PlayerMaxHp);

            Vector3 hitPosition = launcher != null && launcher.CollectTarget != null
                ? launcher.CollectTarget.position
                : launcher != null ? launcher.PlayerVisual.position : transform.position;
            CombatEvents.PlayerHit?.Invoke(hitPosition);

            if (_playerHp <= 0)
                EndGame(false);
        }

        /// <summary>
        /// 게임을 일시정지하고 3택지 선택 상태로 전환합니다.
        /// </summary>
        /// <param name="choices">노출할 선택지 목록</param>
        public void EnterSelection(SkillChoice[] choices)
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Selecting;
            GameTime.Pause();

            GameplayEvents.SkillChoicesReady?.Invoke(choices);
        }

        /// <summary>
        /// 3택지 선택을 마치고 플레이 상태로 복귀합니다.
        /// </summary>
        public void ResumeFromSelection()
        {
            if (State != GameState.Selecting)
                return;

            State = GameState.Playing;
            GameTime.Resume();

            GameplayEvents.SelectionResumed?.Invoke();
        }

        /// <summary>
        /// 게임을 종료합니다. 승리 시에는 <see cref="resultPopupDelay"/>초 뒤에 결과 팝업을 띄워
        /// 클리어 연출(마지막 처치 FX 등)이 먼저 재생되게 합니다. 패배 시에는 실제 플레이어를 숨기고
        /// 사망 연출 전용 대역(<see cref="deathVisual"/>)에서 사망 시점의 좌우 방향에 맞는 Dead
        /// 애니메이션을 재생시킨 뒤, 그 연출이 끝나고 <see cref="deadResultPopupDelay"/>초를 더
        /// 기다렸다가 결과 팝업을 띄웁니다.
        /// </summary>
        /// <param name="won">승리 여부</param>
        public void EndGame(bool won)
        {
            if (State == GameState.Won || State == GameState.Lost)
                return;

            State = won ? GameState.Won : GameState.Lost;

            if (won)
            {
                ShowResultAfterDelayAsync(won, resultPopupDelay).Forget();
                return;
            }

            PlayDeathVisual();
            float deadAnimationDuration = deathVisual != null ? deathVisual.ClipLength : 0f;
            ShowResultAfterDelayAsync(won, deadAnimationDuration + deadResultPopupDelay).Forget();
        }

        /// <summary>
        /// 실제 플레이어의 스프라이트를 숨기고, 사망 시점의 좌우 방향 그대로 사망 연출 전용 대역을
        /// 같은 위치에서 재생합니다. 대역은 방향별로 미리 좌우 대칭 처리된 애니메이션(Dead / Dead_Left)을
        /// 선택 재생하므로, 실제 플레이어처럼 트랜스폼을 반전시키지 않아도 회전 애니메이션이 왜곡되지
        /// 않습니다.
        /// </summary>
        private void PlayDeathVisual()
        {
            bool facingRight = launcher.PlayerVisual.localScale.x >= 0f;

            foreach (SpriteRenderer spriteRenderer in launcher.PlayerVisual.GetComponentsInChildren<SpriteRenderer>())
                spriteRenderer.enabled = false;

            deathVisual?.PlayAt(launcher.PlayerVisual.position, facingRight);
        }

        private async UniTaskVoid ShowResultAfterDelayAsync(bool won, float delaySeconds)
        {
            // WaitForSeconds와 동일하게 timeScale의 영향을 받는 지연(기본 DelayType.DeltaTime)이다.
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: destroyCancellationToken);

            GameTime.Pause();
            GameplayEvents.GameEnded?.Invoke(won);
        }
    }
}
