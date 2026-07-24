using BounceHeroes.Core;
using System;
using System.Collections.Generic;
using BounceHeroes.Data;
using BounceHeroes.UI.UIViews;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 모든 UIView의 생명주기와 오버레이 전환을 관리합니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset skillCardTemplate;
        [SerializeField] private VisualTreeAsset slotItemTemplate;

        [SerializeField, Tooltip("코드로 만든 라벨(결과 카드/리더보드)에 적용할 한글 지원 폰트입니다.")]
        private Font uiFont;

        [SerializeField, Tooltip("Main 진입 후 화면을 검게 덮은 채 대기했다가 페이드 아웃을 시작하기까지의 지연(초)입니다.")]
        private float transitionRevealDelay = 1f;

        [SerializeField, Tooltip("인트로 트랜지션이 걷힌 뒤 실제 게임 플레이가 시작되기까지의 대기 시간(초)입니다.")]
        private float gameStartDelay = 1.5f;

        [SerializeField, Tooltip("인트로 트랜지션 시작 시 재생할 사운드 id입니다.")]
        private AudioId transitionSfx = AudioId.Transition;

        private const string HudBarName = "HudBar";
        private const string SkillSelectOverlayName = "SkillSelectOverlay";
        private const string ResultOverlayName = "ResultOverlay";
        private const string PauseOverlayName = "PauseOverlay";
        private const string TransitionOverlayName = "TransitionOverlay";
        private const string HomeSceneName = "Home";

        private const string BlurOverlayViewName = "blur-overlay";

        private readonly List<UIView> _allViews = new List<UIView>();

        private UIDocument _uiDocument;
        private HudBarView _hudBarView;
        private SkillSelectView _skillSelectView;
        private ResultView _resultView;
        private PauseView _pauseView;
        private ScreenBlurView _screenBlurView;
        private TransitionView _transitionView;

        private IAudioService _audio;

        private IReadOnlyList<SkillLoadoutEntry> _activeLoadout = new List<SkillLoadoutEntry>();
        private IReadOnlyList<SkillLoadoutEntry> _passiveLoadout = new List<SkillLoadoutEntry>();

        [Inject]
        public void Construct(IAudioService audio)
        {
            _audio = audio;
        }

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            SetupViews(_uiDocument.rootVisualElement);
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();

            foreach (UIView view in _allViews)
            {
                view.Dispose();
            }

            _allViews.Clear();
        }

        private void SetupViews(VisualElement root)
        {
            // 코드로 만든 라벨(결과 카드·리더보드)이 한글을 표시하도록 루트에 기본 폰트를 지정한다.
            // UXML에서 폰트를 명시한 요소는 그 값이 우선하므로 영향받지 않는다.
            if (uiFont != null)
                root.style.unityFontDefinition = new StyleFontDefinition(uiFont);

            var hudBarRoot = root.Q<VisualElement>(HudBarName);
            if (hudBarRoot != null) 
                _hudBarView = new HudBarView(hudBarRoot);

            var skillSelectOverlayRoot = root.Q<VisualElement>(SkillSelectOverlayName);
            if (skillSelectOverlayRoot != null) 
                _skillSelectView = new SkillSelectView(skillSelectOverlayRoot, skillCardTemplate, slotItemTemplate);
            
            _resultView = new ResultView(root.Q<VisualElement>(ResultOverlayName));

            var pauseOverlayRoot = root.Q<VisualElement>(PauseOverlayName);
            if (pauseOverlayRoot != null)
                _pauseView = new PauseView(pauseOverlayRoot);

            VisualElement blurOverlay = root.Q<VisualElement>(BlurOverlayViewName);
            if (blurOverlay != null)
                _screenBlurView = new ScreenBlurView(blurOverlay, Camera.main);

            VisualElement transitionOverlay = root.Q<VisualElement>(TransitionOverlayName);
            if (transitionOverlay != null)
                _transitionView = new TransitionView(transitionOverlay);

            _allViews.Add(_hudBarView);
            _allViews.Add(_skillSelectView);
            _allViews.Add(_resultView);
            _allViews.Add(_pauseView);
            if (_transitionView != null)
                _allViews.Add(_transitionView);

            foreach (UIView view in _allViews)
            {
                view.Initialize();
            }

            _hudBarView.Show();

            PlayIntroTransition();
        }

        // 씬 진입 시 화면을 덮고 있던 트랜지션을 페이드 아웃(_Progress 0→1)으로 걷어내고,
        // 걷힌 뒤 gameStartDelay만큼 기다렸다가 게임 플레이 시작을 알린다.
        private void PlayIntroTransition()
        {
            if (_transitionView == null)
            {
                GameUIEvents.GameStartRequested?.Invoke();
                return;
            }

            // 첫 프레임부터 화면을 검게 덮어 둔다. 그래야 페이드 아웃 시작 전에 밝은 게임 화면이 새어 보이지 않는다.
            _transitionView.Cover();
            RevealAfterDelayAsync().Forget();
        }

        // Main 진입 직후에는 로딩 스파이크로 프레임 delta가 크게 튄다. 그 프레임에 트윈을 시작하면 LMotion이
        // 큰 delta를 한 번에 소비해 페이드가 순식간에 끝난다. 화면을 Cover()로 덮은 채 transitionRevealDelay만큼
        // 대기해 delta가 안정된 뒤 페이드 아웃을 시작한다.
        // DelayType.Realtime(Stopwatch 기반)을 쓴다. UnscaledDeltaTime은 스파이크 프레임의 큰 delta를 누적해
        // 대기가 한 프레임에 끝나버릴 수 있어(=대기가 무시됨), 벽시계 기준 Realtime이 스파이크에 면역이다.
        private async UniTaskVoid RevealAfterDelayAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(transitionRevealDelay),
                DelayType.Realtime, cancellationToken: destroyCancellationToken);

            _audio?.PlaySfx(transitionSfx);
            _transitionView.PlayFadeOut(() => BeginGameAfterDelayAsync().Forget());
        }

        private async UniTaskVoid BeginGameAfterDelayAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(gameStartDelay),
                DelayType.UnscaledDeltaTime, cancellationToken: destroyCancellationToken);
            GameUIEvents.GameStartRequested?.Invoke();
        }

        private void SubscribeToEvents()
        {
            GameplayEvents.SkillChoicesReady += OnSkillChoicesReady;
            GameplayEvents.SkillLoadoutChanged += OnSkillLoadoutChanged;
            GameplayEvents.RunCompleted += OnRunCompleted;
            GameplayEvents.LeaderboardReady += OnLeaderboardReady;
            GameUIEvents.SkillChoiceSelected += OnSkillChoiceSelected;
            GameUIEvents.PauseRequested += OnPauseRequested;
            GameUIEvents.PauseCloseRequested += OnPauseCloseRequested;
            GameUIEvents.ExitToHomeRequested += OnExitToHomeRequested;
        }

        private void UnsubscribeFromEvents()
        {
            GameplayEvents.SkillChoicesReady -= OnSkillChoicesReady;
            GameplayEvents.SkillLoadoutChanged -= OnSkillLoadoutChanged;
            GameplayEvents.RunCompleted -= OnRunCompleted;
            GameplayEvents.LeaderboardReady -= OnLeaderboardReady;
            GameUIEvents.SkillChoiceSelected -= OnSkillChoiceSelected;
            GameUIEvents.PauseRequested -= OnPauseRequested;
            GameUIEvents.PauseCloseRequested -= OnPauseCloseRequested;
            GameUIEvents.ExitToHomeRequested -= OnExitToHomeRequested;
        }

        private void OnSkillChoicesReady(SkillChoice[] choices)
        {
            _hudBarView?.ApplyBlur();
            _screenBlurView?.Show();
            _skillSelectView?.ShowChoices(choices, _activeLoadout, _passiveLoadout);
        }

        private void OnSkillLoadoutChanged(
            IReadOnlyList<SkillLoadoutEntry> activeLoadout,
            IReadOnlyList<SkillLoadoutEntry> passiveLoadout)
        {
            _activeLoadout = activeLoadout;
            _passiveLoadout = passiveLoadout;
        }

        private void OnSkillChoiceSelected(int index)
        {
            _hudBarView?.RemoveBlur();
            _screenBlurView?.Hide();
            _skillSelectView?.Hide();
        }

        private void OnRunCompleted(RunResult result)
        {
            _screenBlurView?.Show();
            _skillSelectView?.Hide();
            _resultView.ShowResult(result.Won, result);
        }

        private void OnLeaderboardReady(
            IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry self)
        {
            _resultView.ShowLeaderboard(top, self);
        }

        private void OnPauseRequested()
        {
            GameTime.Pause();
            _hudBarView?.ApplyBlur();
            _screenBlurView?.Show();
            _pauseView?.Show();
        }

        private void OnPauseCloseRequested()
        {
            _pauseView?.Hide();
            _screenBlurView?.Hide();
            _hudBarView?.RemoveBlur();
            GameTime.Resume();
        }

        private void OnExitToHomeRequested()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(HomeSceneName);
        }
    }
}
