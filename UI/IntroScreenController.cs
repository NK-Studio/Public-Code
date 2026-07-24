using System;
using BounceHeroes.Core;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Controls the Intro screen loading flow and first-run nickname popup.
    /// </summary>
    public sealed class IntroScreenController : MonoBehaviour
    {
        [SerializeField, Tooltip("SFX id played when the nickname popup confirm button is clicked.")]
        private AudioId nicknameConfirmSfx = AudioId.UiClick;

        private const string BackgroundName = "intro-screen__background";
        private const string TitleName = "intro-screen__title";
        private const string FooterName = "intro-screen__footer";
        private const string StatusLabelName = "intro-screen__status-label";
        private const string FillName = "intro-screen__progress-bar-fill";
        private const string PercentLabelName = "intro-screen__percent-label";
        private const string VersionLabelName = "intro-screen__version-number";
        private const string NicknamePopupOverlayName = "NicknamePopupOverlay";
        private const string BlurredClassName = "intro-screen__blurred";

        private const string NicknameKey = "Player.Nickname";
        private const string NextSceneName = "Home";

        private const float TitleStartTranslateY = -430f;
        private const float TitleStartScale = 0.6f;
        private const float TitleStartAngle = -8f;
        private const float TitleDropDuration = 0.75f;
        private const float TitleFadeDuration = 0.28f;
        private const float TitleScaleDuration = 0.55f;
        private const float TitleTiltDuration = 0.6f;

        private const float FooterStartTranslateY = 60f;
        private const float FooterEnterDuration = 0.45f;
        private const float FooterEnterDelay = 0.35f;

        private const float MinLoadDuration = 4f;
        private const float ProgressFollowSpeed = 1.4f;
        private const float EmptyBarRevealDelay = FooterEnterDelay + FooterEnterDuration + 0.35f;

        private static readonly string[] StatusMessages =
        {
            "캐릭터 깨우는 중 ...",
            "볼 5개 챙기는 중...",
            "신발 신는 중...",
            "곧 모험 시작합니다 !",
        };

        private readonly CompositeMotionHandle _motions = new CompositeMotionHandle();

        private VisualElement _background;
        private VisualElement _title;
        private VisualElement _footer;
        private Label _statusLabel;
        private VisualElement _fill;
        private Label _percentLabel;
        private Label _versionLabel;

        private NicknamePopupView _nicknamePopupView;
        private ILeaderboardService _leaderboard;
        private IAudioService _audio;
        private bool _nicknameConfirmed;

        private int _currentStatusIndex = -1;

        [Inject]
        public void Construct(IAudioService audio, ILeaderboardService leaderboard)
        {
            _audio = audio;
            _leaderboard = leaderboard;
        }

        private void Start()
        {
            var document = FindAnyObjectByType<UIDocument>();

            VisualElement root = document.rootVisualElement;
            _background = root.Q<VisualElement>(BackgroundName);
            _title = root.Q<VisualElement>(TitleName);
            _footer = root.Q<VisualElement>(FooterName);
            _statusLabel = root.Q<Label>(StatusLabelName);
            _fill = root.Q<VisualElement>(FillName);
            _percentLabel = root.Q<Label>(PercentLabelName);
            _versionLabel = root.Q<Label>(VersionLabelName);

            if (_versionLabel != null)
                _versionLabel.text = $"Ver. {Application.version}";

            _nicknamePopupView = new NicknamePopupView(root.Q<VisualElement>(NicknamePopupOverlayName));
            _nicknamePopupView.Initialize();
            _nicknamePopupView.Confirmed += OnNicknameConfirmed;
            IntroEvents.NicknameConfirmClicked += PlayNicknameConfirmSfx;

            root.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
        }

        private void OnDisable()
        {
            _motions.Cancel();

            if (_nicknamePopupView != null)
                _nicknamePopupView.Confirmed -= OnNicknameConfirmed;

            IntroEvents.NicknameConfirmClicked -= PlayNicknameConfirmSfx;
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            ((VisualElement)evt.target).UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

            PlayTitleIntro();
            PlayFooterIntro();
            RunLoadingAsync().Forget();
        }

        private void PlayTitleIntro()
        {
            if (_title == null)
                return;

            _title.style.translate = new Translate(0f, TitleStartTranslateY);
            _title.style.scale = new Scale(Vector2.one * TitleStartScale);
            _title.style.rotate = new Rotate(new Angle(TitleStartAngle));
            _title.style.opacity = 0f;

            _motions.Add(
                LMotion.Create(new Vector2(0f, TitleStartTranslateY), Vector2.zero, TitleDropDuration)
                    .WithEase(Ease.OutBounce)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleTranslate(_title));

            _motions.Add(
                LMotion.Create(0f, 1f, TitleFadeDuration)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(_title, static (value, title) => title.style.opacity = value));

            _motions.Add(
                LMotion.Create(Vector3.one * TitleStartScale, Vector3.one, TitleScaleDuration)
                    .WithEase(Ease.OutBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleScale(_title));

            _motions.Add(
                LMotion.Create(TitleStartAngle, 0f, TitleTiltDuration)
                    .WithEase(Ease.OutBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleRotate(_title));
        }

        private void PlayFooterIntro()
        {
            if (_footer == null)
                return;

            _footer.style.translate = new Translate(0f, FooterStartTranslateY);
            _footer.style.opacity = 0f;

            _motions.Add(
                LMotion.Create(new Vector2(0f, FooterStartTranslateY), Vector2.zero, FooterEnterDuration)
                    .WithDelay(FooterEnterDelay)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleTranslate(_footer));

            _motions.Add(
                LMotion.Create(0f, 1f, FooterEnterDuration)
                    .WithDelay(FooterEnterDelay)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(_footer, static (value, footer) => footer.style.opacity = value));
        }

        private async UniTaskVoid RunLoadingAsync()
        {
            ApplyProgress(0f);

            AsyncOperation op = SceneManager.LoadSceneAsync(NextSceneName);
            op.allowSceneActivation = false;

            await UniTask.Delay(TimeSpan.FromSeconds(EmptyBarRevealDelay),
                Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
                cancellationToken: destroyCancellationToken);

            float displayed = 0f;
            float elapsed = 0f;

            while (displayed < 0.999f)
            {
                elapsed += Time.unscaledDeltaTime;
                float sceneProgress = Mathf.Clamp01(op.progress / 0.9f);
                float timeProgress = Mathf.Clamp01(elapsed / MinLoadDuration);
                float target = Mathf.Min(sceneProgress, timeProgress);

                displayed = Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime * ProgressFollowSpeed);
                ApplyProgress(displayed);

                await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
            }

            ApplyProgress(1f);
            await UniTask.Delay(TimeSpan.FromMilliseconds(250),
                Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
                cancellationToken: destroyCancellationToken);

            await LoginAndCollectNicknameAsync();

            op.allowSceneActivation = true;
        }

        private async UniTask LoginAndCollectNicknameAsync()
        {
            if (_leaderboard != null)
            {
                try
                {
                    await _leaderboard.LoginAsync();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[IntroScreenController] Login failed: {e.Message}");
                }
            }

            if (PlayerPrefs.HasKey(NicknameKey))
                return;

            await ShowNicknamePopupAsync();
        }

        private async UniTask ShowNicknamePopupAsync()
        {
            _nicknameConfirmed = false;

            SetIntroBlurred(true);
            _nicknamePopupView.Open(string.Empty);

            await UniTask.WaitUntil(() => _nicknameConfirmed, cancellationToken: destroyCancellationToken);

            _nicknamePopupView.Hide();
            SetIntroBlurred(false);
        }

        private void OnNicknameConfirmed(string nickname)
        {
            PlayerPrefs.SetString(NicknameKey, nickname);
            PlayerPrefs.Save();
            _nicknameConfirmed = true;
        }

        private void PlayNicknameConfirmSfx() => _audio?.PlaySfx(nicknameConfirmSfx);

        private void SetIntroBlurred(bool blurred)
        {
            ToggleBlur(_background, blurred);
            ToggleBlur(_title, blurred);
            ToggleBlur(_footer, blurred);
            ToggleBlur(_versionLabel, blurred);
        }

        private static void ToggleBlur(VisualElement element, bool blurred)
        {
            if (element == null)
                return;

            if (blurred)
                element.AddToClassList(BlurredClassName);
            else
                element.RemoveFromClassList(BlurredClassName);
        }

        private void ApplyProgress(float progress)
        {
            if (_fill != null)
                _fill.style.width = Length.Percent(progress * 100f);

            if (_percentLabel != null)
                _percentLabel.text = $"{Mathf.RoundToInt(progress * 100f)}%";

            UpdateStatusMessage(progress);
        }

        private void UpdateStatusMessage(float progress)
        {
            if (_statusLabel == null)
                return;

            int index = Mathf.Clamp((int)(progress * StatusMessages.Length), 0, StatusMessages.Length - 1);
            if (index == _currentStatusIndex)
                return;

            _currentStatusIndex = index;
            _statusLabel.text = StatusMessages[index];
        }
    }
}
