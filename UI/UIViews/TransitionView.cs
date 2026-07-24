using System;
using LitMotion;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 화면 전체를 덮는 디졸브 트랜지션 연출을 담당하는 View입니다.
    /// Transition 엘리먼트에 지정된 머티리얼을 복제·캐싱한 뒤 <c>_Progress</c> 값을 애니메이션합니다.
    /// - 페이드 인: _Progress 1 → 0 (투명 → 화면을 덮음)
    /// - 페이드 아웃: _Progress 0 → 1 (화면을 덮음 → 투명하게 걷힘)
    /// duration이 끝나면 전달받은 콜백이 호출되므로, 씬 로드 등 후속 동작을 확장성 있게 이어붙일 수 있습니다.
    /// </summary>
    public sealed class TransitionView : UIView
    {
        private const string TransitionElementName = "transition";
        private const string ProgressPropertyName = "_Progress";
        private const float DefaultDuration = 0.8f;

        private VisualElement _transition;
        private Material _runtimeMaterial;
        private MotionHandle _motion;

        // 우리가 원하는 현재 _Progress 값. 머티리얼이 아직 준비되지 않았어도 값을 기억해 두었다가,
        // 준비되는 즉시(EnsureRuntimeMaterial) 적용한다. 이렇게 해야 Cover() 등이 머티리얼 준비 타이밍과
        // 무관하게 항상 화면 상태를 결정할 수 있고, 원본 에셋에 저장된 값에도 영향받지 않는다.
        private float _progress;

        /// <param name="root">Transition 템플릿 인스턴스(또는 transition 엘리먼트)입니다.</param>
        public TransitionView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            // HomeScreen에서는 Transition 템플릿 인스턴스가 root로 넘어오므로 내부의 실제 머티리얼 엘리먼트를 찾는다.
            _transition = _root.Q<VisualElement>(TransitionElementName) ?? _root;

            // 대기 상태에서는 오버레이가 입력을 가로채지 않도록 한다.
            _transition.pickingMode = PickingMode.Ignore;
            _root.pickingMode = PickingMode.Ignore;

            // 머티리얼은 레이아웃(resolvedStyle)을 기다리지 않고, UXML이 지정한 인라인 스타일에서 곧바로 읽어
            // 복제한다. 이렇게 하면 첫 프레임부터 우리가 _Progress를 완전히 통제하므로, UXML의 prop 값이나
            // 레이아웃 타이밍과 무관하게 페이드 아웃 시작 순간부터 화면이 확실히 덮여 있게 된다.
            EnsureRuntimeMaterial(_transition.style.unityMaterial.value.material);

            // 인라인 스타일에서 머티리얼을 읽지 못하는 환경을 대비한 폴백: 레이아웃이 잡히면 resolvedStyle에서 다시 시도한다.
            if (_runtimeMaterial == null)
                _transition.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public override void Dispose()
        {
            _transition.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_motion.IsActive())
                _motion.Cancel();

            if (_runtimeMaterial != null)
                UnityEngine.Object.Destroy(_runtimeMaterial);
        }

        /// <summary>
        /// 애니메이션 없이 즉시 화면을 완전히 덮은 상태(_Progress 0)로 표시합니다.
        /// 페이드 아웃을 한 프레임 뒤에 시작할 때, 그 사이 프레임에 반대편(밝은 화면)이 새어 보이는 것을 막습니다.
        /// </summary>
        public void Cover()
        {
            if (_motion.IsActive())
                _motion.Cancel();

            SetProgress(0f);
            Show();
        }

        /// <summary>
        /// 페이드 인(_Progress 1 → 0)을 재생합니다. duration이 끝나면 <paramref name="onComplete"/>가 호출됩니다.
        /// </summary>
        public void PlayFadeIn(Action onComplete = null, float duration = DefaultDuration, Ease ease = Ease.OutExpo)
        {
            Play(1f, 0f, duration, onComplete, ease);
        }

        /// <summary>
        /// 페이드 아웃(_Progress 0 → 1)을 재생합니다. duration이 끝나면 <paramref name="onComplete"/>가 호출됩니다.
        /// </summary>
        public void PlayFadeOut(Action onComplete = null, float duration = DefaultDuration, Ease ease = Ease.InExpo)
        {
            Play(0f, 1f, duration, onComplete, ease);
        }

        private void Play(float from, float to, float duration, Action onComplete, Ease ease)
        {
            // 시작 상태를 먼저 확정(SetProgress)한 뒤 보여줘, 반대편이 한 프레임 번쩍이는 일이 없게 한다.
            SetProgress(from);
            Show();

            if (_motion.IsActive())
                _motion.Cancel();

            _motion = LMotion.Create(from, to, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => onComplete?.Invoke())
                .Bind(this, static (value, self) => self.SetProgress(value));
        }

        private void OnGeometryChanged(GeometryChangedEvent _)
        {
            EnsureRuntimeMaterial(_transition.resolvedStyle.unityMaterial.material);
        }

        /// <summary>
        /// 지정된 트랜지션 머티리얼을 복제해 이 인스턴스 전용으로 캐싱합니다.
        /// 원본 에셋이 아닌 복제본만 갱신하므로 다른 사용처에 영향을 주지 않습니다. (HudBarView와 동일한 취지)
        /// </summary>
        private void EnsureRuntimeMaterial(Material sourceMaterial)
        {
            if (_runtimeMaterial != null || sourceMaterial == null)
                return;

            _runtimeMaterial = UnityEngine.Object.Instantiate(sourceMaterial);

            // prop() 오버라이드 없이 재지정해, 이후 알파는 전적으로 우리가 SetFloat하는 _Progress가 결정하게 한다.
            _transition.style.unityMaterial = new MaterialDefinition(_runtimeMaterial);

            // 원본 에셋에 저장된 값이 무엇이든, 준비 즉시 우리가 원하는 현재 progress로 덮어써 화면 상태를 확정한다.
            _runtimeMaterial.SetFloat(ProgressPropertyName, _progress);
            _transition.MarkDirtyRepaint();
        }

        private void SetProgress(float value)
        {
            _progress = value;

            if (_runtimeMaterial == null)
                return;

            _runtimeMaterial.SetFloat(ProgressPropertyName, value);
            _transition.MarkDirtyRepaint();
        }
    }
}
