using System;
using BounceHeroes.Core;
using LitMotion;
using LitMotion.Extensions;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlayerHpBarWorld : MonoBehaviour, INotifyBindablePropertyChanged
    {
        [SerializeField] private float revealPopDuration = 0.4f;

        private UIDocument _uiDocument;
        private VisualElement _container;
        private VisualElement _fill;
        private MotionHandle _revealTween;
        private bool _revealed;
        private string _hpText = "100";

        /// <summary>player-hp-bar__label의 text가 UXML Bindings를 통해 구독하는 값입니다.</summary>
        [CreateProperty]
        public string HpText
        {
            get => _hpText;
            private set
            {
                if (_hpText == value)
                    return;

                _hpText = value;
                propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(nameof(HpText)));
            }
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _uiDocument.rootVisualElement;
            root.dataSource = this;
            _container = root.Q<VisualElement>("player-hp-bar");
            _fill = root.Q<VisualElement>("player-hp-bar__fill");

            // 몬스터가 하단에 도달하기 전까지 플레이어는 피격당하지 않으므로, 그때까지 풀피 HP 바를
            // 미리 보여줄 필요가 없다. 첫 피격 시 OnPlayerHit에서 노출한다.
            _revealed = false;
            transform.localScale = Vector3.one;
            if (_container != null)
                _container.style.display = DisplayStyle.None;

            GameplayEvents.PlayerHpChanged += OnPlayerHpChanged;
            CombatEvents.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            GameplayEvents.PlayerHpChanged -= OnPlayerHpChanged;
            CombatEvents.PlayerHit -= OnPlayerHit;
            _revealTween.TryCancel();
        }

        private void OnPlayerHpChanged(int current, int max)
        {
            // 100% = 풀피, 0% = 사망. fill이 절대배치+양쪽 앵커 고정이었을 때는 width가
            // 무시되었으나, background 안쪽 flex 자식으로 옮기면서 정상적으로 폭이 반영된다.
            float ratio = max > 0 ? (float)current / max : 0f;
            _fill.style.width = Length.Percent(ratio * 100f);
            HpText = current.ToString();
        }

        private void OnPlayerHit(Vector3 position)
        {
            RevealHpBar();
        }

        /// <summary>첫 피격 시 한 번만 팝인 연출과 함께 HP 바를 드러냅니다. 이후로는 다시 숨기지 않습니다.</summary>
        private void RevealHpBar()
        {
            if (_revealed || _container == null)
                return;

            _revealed = true;
            _container.style.display = DisplayStyle.Flex;

            _revealTween.TryCancel();
            transform.localScale = Vector3.zero;
            _revealTween = LMotion.Create(Vector3.zero, Vector3.one, revealPopDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(transform);
        }
    }
}
