using System;
using System.Text;
using BounceHeroes.Data;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 몬스터 타격 지점에 월드 스페이스로 떠오르는 스프라이트 숫자 데미지 팝업입니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WorldDamagePopup : MonoBehaviour
    {
        private const float RiseDuration = 0.18f;
        private const float FallDuration = 0.52f;
        private const float RiseHeight = 0.5f;
        private const float FallHeight = 0.18f;
        private const float PunchScale = 0.35f;
        private const float MaxDrift = 0.15f;

        [Serializable]
        private struct PopupStyle
        {
            public Color tint;
            public float fontSize;
        }

        [SerializeField] private PopupStyle normalStyle = new PopupStyle { tint = Color.white, fontSize = 50f };
        [SerializeField] private PopupStyle critStyle = new PopupStyle { tint = new Color(1f, 31f / 255f, 31f / 255f), fontSize = 70f };
        [SerializeField] private PopupStyle effectStyle = new PopupStyle { tint = new Color(1f, 141f / 255f, 0f), fontSize = 50f };

        private Label _label;
        private MotionHandle _tween;

        private UIDocument _uiDocument;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            // World Space UIDocument는 비활성화 시 패널을 해제하고 활성화 시 다시 만들기 때문에,
            // 풀링으로 재사용될 때마다 Label을 새로 찾고 정렬 순서를 다시 적용해야 한다.
            _label = _uiDocument.rootVisualElement.Q<Label>("damage-popup__label");

            Renderer uiRenderer = GetComponent<Renderer>();

            if (uiRenderer != null)
                uiRenderer.sortingOrder = 1000;
        }
        
        /// <summary>
        /// 팝업을 재생합니다. 완료되면 콜백으로 자신을 반환해 풀링할 수 있게 합니다.
        /// </summary>
        /// <param name="amount">표시할 데미지 수치</param>
        /// <param name="type">팝업 유형 (색상/크기 결정)</param>
        /// <param name="onComplete">재생 완료 시 호출되는 콜백</param>
        public void Play(int amount, DamagePopupType type, Action<WorldDamagePopup> onComplete)
        {
            _tween.TryCancel();

            PopupStyle style = type switch
            {
                DamagePopupType.Crit => critStyle,
                DamagePopupType.Effect => effectStyle,
                _ => normalStyle
            };

            _label.style.fontSize = style.fontSize;
            _label.text = BuildSpriteText(amount, style.tint);

            _label.style.opacity = 1f;
            transform.localScale = Vector3.one;

            Vector3 startPosition = transform.position;
            float driftX = UnityEngine.Random.Range(-MaxDrift, MaxDrift);
            Vector3 peakPosition = startPosition + new Vector3(driftX * 0.3f, RiseHeight, 0f);
            Vector3 restPosition = peakPosition + new Vector3(driftX * 0.7f, -FallHeight, 0f);

            // 튀어오르는 임팩트: 빠르게 솟구치며 살짝 커진다.
            MotionHandle riseMove = LMotion.Create(startPosition, peakPosition, RiseDuration)
                .WithEase(Ease.OutCubic)
                .BindToPosition(transform);

            MotionHandle risePunch = LMotion.Punch.Create(Vector3.one, Vector3.one * PunchScale, RiseDuration)
                .WithFrequency(6)
                .WithDampingRatio(0.6f)
                .BindToLocalScale(transform);

            // 중력에 끌리듯 가속하며 내려앉고, 마지막 구간에서만 페이드.
            MotionHandle fallMove = LMotion.Create(peakPosition, restPosition, FallDuration)
                .WithEase(Ease.InQuad)
                .BindToPosition(transform);

            MotionHandle fade = LMotion.Create(1f, 0f, FallDuration * 0.6f)
                .BindToStyleOpacity(_label);

            _tween = LSequence.Create()
                .Append(riseMove)
                .Join(risePunch)
                .Append(fallMove)
                .Insert(RiseDuration + FallDuration * 0.4f, fade)
                .Run(builder => builder.WithOnComplete(() => onComplete?.Invoke(this)));
        }

        private static string BuildSpriteText(int amount, Color tint)
        {
            string hex = ColorUtility.ToHtmlStringRGB(tint);

            string digits = amount.ToString();
            StringBuilder builder = new StringBuilder(digits.Length * 24);

            foreach (char digit in digits)
                builder.Append("<sprite=").Append(digit).Append(" color=#").Append(hex).Append('>');

            return builder.ToString();
        }
    }
}
