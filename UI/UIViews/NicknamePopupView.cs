using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Home 최초 진입 시 닉네임 입력을 유도하는 팝업입니다. Blur 오버레이와 함께 사용되며,
    /// 확인을 누르면 <see cref="Confirmed"/>를 발생시키고 스스로는 닫지 않습니다(호출자가 Hide 호출).
    /// </summary>
    public sealed class NicknamePopupView : UIView
    {
        private const float PopScale = 0.85f;
        private const float PopDuration = 0.3f;

        private readonly CompositeMotionHandle _motions = new CompositeMotionHandle();

        private VisualElement _panel;
        private TextField _input;
        private Button _confirmButton;

        /// <summary>확인 버튼(또는 Enter)으로 닉네임 입력을 완료했을 때 발생합니다.</summary>
        public event Action<string> Confirmed;

        public NicknamePopupView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _panel = _root.Q<VisualElement>("nickname-popup__panel");
            _input = _root.Q<TextField>("nickname-popup__input");
            _confirmButton = _root.Q<Button>("nickname-popup__confirm-button");

            // TextField는 label을 지정하지 않아도 빈 라벨 영역을 차지하므로, 입력칸이 중앙 정렬되도록 숨긴다.
            Label phantomLabel = _input.Q<Label>(className: "unity-base-field__label");
            if (phantomLabel != null)
                phantomLabel.style.display = DisplayStyle.None;
        }

        protected override void RegisterCallbacks()
        {
            _confirmButton.clicked += OnConfirmClicked;
            _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
        }

        public override void Dispose()
        {
            _confirmButton.clicked -= OnConfirmClicked;
            _input.UnregisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            _motions.Cancel();
        }

        /// <summary>팝업을 열고 입력칸에 기존 닉네임(있다면)을 채운 뒤 포커스를 준다.</summary>
        public void Open(string currentNickname)
        {
            _input.SetValueWithoutNotify(currentNickname);
            Show();
            _input.Focus();
        }

        /// <summary>팝업을 표시하며 살짝 튀어 들어오는 등장 연출을 재생한다.</summary>
        public override void Show()
        {
            base.Show();
            PlayPopIn();
        }

        private void PlayPopIn()
        {
            _motions.Cancel();

            _panel.style.scale = new Scale(Vector2.one * PopScale);
            _panel.style.opacity = 0f;

            _motions.Add(
                LMotion.Create(PopScale, 1f, PopDuration)
                    .WithEase(Ease.OutBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(_panel, static (x, panel) => panel.style.scale = new Scale(Vector2.one * x)));

            _motions.Add(
                LMotion.Create(0f, 1f, PopDuration * 0.7f)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(_panel, static (a, panel) => panel.style.opacity = a));
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                OnConfirmClicked();
        }

        private void OnConfirmClicked()
        {
            string nickname = _input.value?.Trim();
            if (string.IsNullOrEmpty(nickname))
                return;

            IntroEvents.NicknameConfirmClicked?.Invoke();
            Confirmed?.Invoke(nickname);
        }
    }
}
