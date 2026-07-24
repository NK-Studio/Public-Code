using BounceHeroes.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 일시정지/설정 팝업의 표시와 입력을 담당합니다. 별도의 닫기 버튼 없이, 팝업 바깥(오버레이)을
    /// 클릭하면 닫히고 팝업 내부 클릭은 전파를 막아 닫히지 않도록 합니다. 배경음악/효과음은
    /// 슬라이더가 아니라 켬(왼쪽)·끔(오른쪽) 토글로 켜고 끕니다.
    /// </summary>
    public sealed class PauseView : UIView
    {
        private const string BgmEnabledKey = "Audio.BgmEnabled";
        private const string SfxEnabledKey = "Audio.SfxEnabled";
        private const string OnLabel = "켬";
        private const string OffLabel = "끔";

        private VisualElement _panel;
        private VisualElement _bgmToggle;
        private VisualElement _sfxToggle;
        private Label _bgmToggleLabel;
        private Label _sfxToggleLabel;
        private Button _resumeButton;
        private Button _exitButton;

        private bool _bgmEnabled;
        private bool _sfxEnabled;

        /// <summary>
        /// PauseView 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="root">일시정지 팝업의 최상위 VisualElement입니다.</param>
        public PauseView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _panel = _root.Q<VisualElement>("pause__panel");
            _bgmToggle = _root.Q<VisualElement>("pause__bgm-toggle");
            _sfxToggle = _root.Q<VisualElement>("pause__sfx-toggle");
            _bgmToggleLabel = _root.Q<Label>("pause__bgm-toggle-label");
            _sfxToggleLabel = _root.Q<Label>("pause__sfx-toggle-label");
            _resumeButton = _root.Q<Button>("pause__resume-button");
            _exitButton = _root.Q<Button>("pause__exit-button");

            _bgmEnabled = PlayerPrefs.GetInt(BgmEnabledKey, 1) == 1;
            _sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;

            ApplyToggleVisual(_bgmToggle, _bgmToggleLabel, _bgmEnabled);
            ApplyToggleVisual(_sfxToggle, _sfxToggleLabel, _sfxEnabled);
        }

        protected override void RegisterCallbacks()
        {
            _root.RegisterCallback<ClickEvent>(OnOverlayClicked);
            _panel.RegisterCallback<ClickEvent>(OnPanelClicked);
            _bgmToggle.RegisterCallback<ClickEvent>(OnBgmToggleClicked);
            _sfxToggle.RegisterCallback<ClickEvent>(OnSfxToggleClicked);
            _resumeButton.clicked += OnResumeClicked;
            _exitButton.clicked += OnExitClicked;
        }

        /// <summary>
        /// 팝업이 등록한 UI 콜백을 해제합니다.
        /// </summary>
        public override void Dispose()
        {
            _root.UnregisterCallback<ClickEvent>(OnOverlayClicked);
            _panel.UnregisterCallback<ClickEvent>(OnPanelClicked);
            _bgmToggle.UnregisterCallback<ClickEvent>(OnBgmToggleClicked);
            _sfxToggle.UnregisterCallback<ClickEvent>(OnSfxToggleClicked);
            _resumeButton.clicked -= OnResumeClicked;
            _exitButton.clicked -= OnExitClicked;
        }

        private void OnPanelClicked(ClickEvent evt)
        {
            // 팝업 내부 클릭은 오버레이까지 전파되지 않게 막아, 팝업이 닫히지 않도록 한다.
            evt.StopPropagation();
        }

        private void OnOverlayClicked(ClickEvent evt)
        {
            GameUIEvents.PauseCloseRequested?.Invoke();
        }

        private void OnBgmToggleClicked(ClickEvent evt)
        {
            _bgmEnabled = !_bgmEnabled;
            PlayerPrefs.SetInt(BgmEnabledKey, _bgmEnabled ? 1 : 0);
            ApplyToggleVisual(_bgmToggle, _bgmToggleLabel, _bgmEnabled);
            GameUIEvents.BgmEnabledChanged?.Invoke(_bgmEnabled);
        }

        private void OnSfxToggleClicked(ClickEvent evt)
        {
            _sfxEnabled = !_sfxEnabled;
            PlayerPrefs.SetInt(SfxEnabledKey, _sfxEnabled ? 1 : 0);
            ApplyToggleVisual(_sfxToggle, _sfxToggleLabel, _sfxEnabled);
            GameUIEvents.SfxEnabledChanged?.Invoke(_sfxEnabled);
        }

        private void OnResumeClicked()
        {
            GameUIEvents.PauseCloseRequested?.Invoke();
        }

        private void OnExitClicked()
        {
            GameUIEvents.ExitToHomeRequested?.Invoke();
        }

        /// <summary>
        /// 토글 노브(켬/끔 하이라이트)의 좌우 위치와 라벨을 켜짐/꺼짐 상태에 맞게 갱신합니다.
        /// 켜짐은 왼쪽(켬), 꺼짐은 오른쪽(끔)입니다.
        /// </summary>
        private void ApplyToggleVisual(VisualElement toggle, Label label, bool enabled)
        {
            toggle.style.alignItems = enabled ? Align.FlexStart : Align.FlexEnd;
            label.text = enabled ? OnLabel : OffLabel;
        }

    }
}
