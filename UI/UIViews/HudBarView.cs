using BounceHeroes.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 상단 HUD의 웨이브 진행(여정 바), 플레이어 체력, 스킬 게이지 표시를 담당합니다.
    /// </summary>
    public sealed class HudBarView : UIView
    {
        private const float JourneyPlayerTranslateXMin = -250f;
        private const float JourneyPlayerTranslateXMax = 228f;
        private const string FillAmountPropertyName = "_FillAmount";

        private VisualElement _journeyMask;
        private VisualElement _journeyPlayer;
        private VisualElement _journeySilhouette;
        private Label _journeyPercent;
        private Label _scoreLabel;
        private Button _pauseButton;

        private Material _silhouetteRuntimeMaterial;
        private bool _isSilhouetteMaterialReady;
        private float _pendingExperienceRatio;

        /// <summary>
        /// HudBarView 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="root">HUD의 최상위 VisualElement입니다.</param>
        public HudBarView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _journeyMask = _root.Q<VisualElement>("hud-bar__journey-mask");
            _journeyPlayer = _root.Q<VisualElement>("hud-bar__journey-player");
            _journeySilhouette = _root.Q<VisualElement>("hud-bar__journey-silhouette");
            _journeyPercent = _root.Q<Label>("hud-bar__journey-percent");
            _scoreLabel = _root.Q<Label>("hud-bar__score");
            _pauseButton = _root.Q<Button>("hud-bar__pause-button");

            _journeySilhouette.RegisterCallback<GeometryChangedEvent>(OnSilhouetteGeometryChanged);
        }

        private void OnSilhouetteGeometryChanged(GeometryChangedEvent _)
        {
            Material styledMaterial = _journeySilhouette.resolvedStyle.unityMaterial.material;
            if (styledMaterial == null)
                return;

            // UI Builder에서 지정한 프로퍼티 값을 그대로 물려받도록 스타일이 적용된 머티리얼을 복제해서
            // 캐싱하고, 원본 에셋이 아닌 이 인스턴스만 직접 갱신한다.
            _silhouetteRuntimeMaterial = Object.Instantiate(styledMaterial);
            _journeySilhouette.style.unityMaterial = new MaterialDefinition(_silhouetteRuntimeMaterial);
            _isSilhouetteMaterialReady = true;

            ApplySilhouetteFill(_pendingExperienceRatio);
        }

        protected override void RegisterCallbacks()
        {
            GameplayEvents.StageProgressChanged += OnStageProgressChanged;
            GameplayEvents.GameEnded += OnGameEnded;
            GameplayEvents.LevelProgressChanged += OnLevelProgressChanged;
            GameplayEvents.ScoreChanged += OnScoreChanged;
            _pauseButton.clicked += OnPauseButtonClicked;
        }

        /// <summary>
        /// HUD가 등록한 이벤트 구독을 해제합니다.
        /// </summary>
        public override void Dispose()
        {
            GameplayEvents.StageProgressChanged -= OnStageProgressChanged;
            GameplayEvents.GameEnded -= OnGameEnded;
            GameplayEvents.LevelProgressChanged -= OnLevelProgressChanged;
            GameplayEvents.ScoreChanged -= OnScoreChanged;
            _pauseButton.clicked -= OnPauseButtonClicked;

            _journeySilhouette.UnregisterCallback<GeometryChangedEvent>(OnSilhouetteGeometryChanged);

            if (_silhouetteRuntimeMaterial != null)
                Object.Destroy(_silhouetteRuntimeMaterial);
        }

        /// <summary>
        /// 스테이지 진행률(사라진 몬스터 수 / 스테이지 전체 몬스터 수)을 여정 바 Width/플레이어 위치/
        /// 퍼센트 라벨에 반영합니다. 웨이브 경계가 아니라 몬스터가 죽거나 바닥에 도달해 사라질 때마다
        /// 갱신되므로, 한 웨이브 내에서도 진행 상황이 부드럽게 채워진다.
        /// </summary>
        private void OnStageProgressChanged(int cleared, int total)
        {
            float ratio = total > 0 ? Mathf.Clamp01((float)cleared / total) : 0f;
            ApplyJourneyProgress(ratio);
        }

        private void OnGameEnded(bool won)
        {
            if (won)
                ApplyJourneyProgress(1f);
        }

        private void OnPauseButtonClicked()
        {
            GameUIEvents.PauseRequested?.Invoke();
        }

        private void OnScoreChanged(long score)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = score.ToString("N0");
        }

        /// <summary>
        /// 실루엣의 _FillAmount는 웨이브 여정 진행률이 아니라, 다음 레벨까지 필요한 경험치
        /// (몬스터 처치 수) 대비 현재 경험치 비율을 표시한다.
        /// </summary>
        private void OnLevelProgressChanged(int level, int experience, int required)
        {
            float ratio = required > 0 ? Mathf.Clamp01((float)experience / required) : 1f;
            ApplySilhouetteFill(ratio);
        }

        private void ApplyJourneyProgress(float ratio)
        {
            _journeyMask.style.width = Length.Percent(ratio * 100f);
            _journeyPlayer.style.translate = new Translate(
                Mathf.Lerp(JourneyPlayerTranslateXMin, JourneyPlayerTranslateXMax, ratio), 0);
            _journeyPercent.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
        }

        private void ApplySilhouetteFill(float ratio)
        {
            _pendingExperienceRatio = ratio;

            if (!_isSilhouetteMaterialReady)
                return;

            // MaterialDefinition은 Material 참조뿐 아니라 자체 프로퍼티 오버라이드(prop() 값)를 들고 있어서,
            // 렌더러는 Material의 값이 아니라 이 구조체에 저장된 오버라이드를 읽는다. 그래서 Material.SetFloat이
            // 아니라 이 구조체의 SetFloat을 호출한 뒤 style에 다시 대입해야 실제로 반영된다.
            _silhouetteRuntimeMaterial.SetFloat(FillAmountPropertyName, ratio);
            _journeySilhouette.MarkDirtyRepaint();
        }
        
        public void RemoveBlur()
        {
            _root.RemoveFromClassList("hud-bar--blur");
        }

        public void ApplyBlur()
        {
            _root.AddToClassList("hud-bar--blur");
        }
    }
}
