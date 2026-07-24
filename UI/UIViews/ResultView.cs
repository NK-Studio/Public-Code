using System.Collections.Generic;
using BounceHeroes.Core;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// ?�공/?�패 결과 ?�업???�시, ?�장 ?�출, ?�수·?�급·?�계 카드 구성, ?�시??종료 ?�력 발행???�당?�니??
    /// </summary>
    public sealed class ResultView : UIView
    {
        private const float PanelStartScale = 0.85f;
        private const float PanelPopDuration = 0.35f;

        private const float HeadStartTranslateY = 360f;
        private const float HeadStartScale = 0.1f;
        private const float HeadPopDuration = 0.5f;
        private const float HeadBounceAngle = 9f;
        private const float HeadTiltDuration = 0.2f;
        private const float HeadSettleDuration = 0.3f;

        private readonly CompositeMotionHandle _defeatMotions = new CompositeMotionHandle();

        private VisualElement _defeatView;
        private VisualElement _successView;
        private VisualElement _defeatPanel;
        private VisualElement _playerHead;
        private VisualElement _successPanel;
        private VisualElement _successPlayerHead;
        private Button _finishButton;
        private Button _replayButton;

        private Button _successFinishButton;
        private Button _successReplayButton;

        /// <summary>
        /// ResultView ?�스?�스�?초기?�합?�다.
        /// </summary>
        /// <param name="root">결과 ?�업??최상??VisualElement?�니??</param>
        public ResultView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _defeatView = _root.Q<VisualElement>("result-overlay__view--defeat");
            _successView = _root.Q<VisualElement>("result-overlay__view--success");
            _defeatPanel = _root.Q<VisualElement>("dead-result__panel");
            _playerHead = _root.Q<VisualElement>("player-head");
            _successPanel = _root.Q<VisualElement>("success-result__panel");
            _successPlayerHead = _root.Q<VisualElement>("success-player-head");
            _finishButton = _root.Q<Button>("finish-button");
            _replayButton = _root.Q<Button>("replay-button");

            _successFinishButton = _root.Q<Button>("success-finish-button");
            _successReplayButton = _root.Q<Button>("success-replay-button");
        }

        protected override void RegisterCallbacks()
        {
            _finishButton.clicked += OnFinishClicked;
            _replayButton.clicked += OnReplayClicked;
            _successFinishButton.clicked += OnFinishClicked;
            _successReplayButton.clicked += OnReplayClicked;
        }

        /// <summary>
        /// 결과 ?�업???�록??UI 콜백�?진행 중인 ?�출???�리?�니??
        /// </summary>
        public override void Dispose()
        {
            _finishButton.clicked -= OnFinishClicked;
            _replayButton.clicked -= OnReplayClicked;
            _successFinishButton.clicked -= OnFinishClicked;
            _successReplayButton.clicked -= OnReplayClicked;
            _defeatMotions.Cancel();
        }

        /// <summary>
        /// ?�패 결과?� ?�수·?�계�??�시?�니?? ?�배 ???�업/?�드 ?�장 ?�출???�생?�니??
        /// </summary>
        /// <param name="won">?�리 ?��?</param>
        /// <param name="result">최종 ?�수·?�계</param>
        public void ShowResult(bool won, RunResult result)
        {
            _defeatView.style.display = won ? DisplayStyle.None : DisplayStyle.Flex;
            _successView.style.display = won ? DisplayStyle.Flex : DisplayStyle.None;

            Show();

            PlayResultIntro(won ? _successPanel : _defeatPanel, won ? _successPlayerHead : _playerHead);
        }

        private void OnFinishClicked()
        {
            GameUIEvents.ExitToHomeRequested?.Invoke();
        }

        private void OnReplayClicked()
        {
            GameUIEvents.RestartRequested?.Invoke();
        }

        /// <summary>
        /// ?�수·?�급·?�계 카드�?코드�?구성???�재 ?�시 중인 뷰에 주입?�니??
        /// ?�리 뷰는 비어 ?�어 버튼???�께 만들???�습?�다(?�배 뷰는 기존 버튼???�용).
        /// </summary>
        public void ShowLeaderboard(IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry self)
        {
            // 성공 결과 상세 UI는 일정상 비활성화한다.
        }

        /// <summary>
        /// ?�배 ?�업???�장 ?�출???�생?�니?? ?�업?� 과하지 ?��? ?��????�업?�로,
        /// ?�드???�래?�서 ?�아?�르�??�짝 ?�겨 ?�착?�는 ?�낌?�로 ?�출?�니??
        /// </summary>
        private void PlayResultIntro(VisualElement panel, VisualElement playerHead)
        {
            _defeatMotions.Cancel();

            panel.style.scale = new Scale(Vector2.one * PanelStartScale);
            _defeatMotions.Add(
                LMotion.Create(PanelStartScale, 1f, PanelPopDuration)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(panel, static (x, targetPanel) => targetPanel.style.scale = new Scale(Vector2.one * x))
            );

            playerHead.style.translate = new Translate(0, HeadStartTranslateY);
            playerHead.style.scale = new Scale(Vector2.one * HeadStartScale);
            playerHead.style.rotate = new Rotate(new Angle(0f));

            _defeatMotions.Add(
                LMotion.Create(new Vector2(0f, HeadStartTranslateY), Vector2.zero, HeadPopDuration)
                    .WithEase(Ease.OutBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleTranslate(playerHead)
            );

            _defeatMotions.Add(
                LMotion.Create(Vector3.one * HeadStartScale, Vector3.one, HeadPopDuration)
                    .WithEase(Ease.OutBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleScale(playerHead)
            );

            MotionHandle tilt = LMotion.Create(0f, HeadBounceAngle, HeadTiltDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToStyleRotate(playerHead);

            MotionHandle settle = LMotion.Create(HeadBounceAngle, 0f, HeadSettleDuration)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToStyleRotate(playerHead);

            _defeatMotions.Add(LSequence.Create()
                .Append(tilt)
                .Append(settle)
                .Run());
        }
    }
}
