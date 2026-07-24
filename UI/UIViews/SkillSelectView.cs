using System.Collections.Generic;
using System.Text.RegularExpressions;
using BounceHeroes.Core;
using BounceHeroes.Data;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 3택지 스킬 선택 오버레이의 카드 생성, 로드아웃 슬롯 표시, 입력 이벤트 발행을 담당합니다.
    /// </summary>
    public sealed class SkillSelectView : UIView
    {
        private const string ActiveSlotClass = "skill-select__loadout-slot--active";
        private const string PassiveSlotClass = "skill-select__loadout-slot--passive";
        private const string FilledSlotModifier = "skill-select__loadout-slot--filled";
        private const string ActiveBackgroundModifier = "skill-card__background--active";
        private const string PassiveBackgroundModifier = "skill-card__background--passive";
        private const string LevelIndicatorClass = "skill-card__level-indicator";
        private const string SelectLevelCount = "skill-select__level-count";
        private const string HighlightColorHex = "FFD176";

        private const int CardRevealBaseDelayMs = 60;
        private const int CardRevealStaggerStepMs = 90;
        private const float CardRevealDuration = 0.45f;
        private const float IndicatorPulseScale = 1.2f;
        private const float IndicatorPulseDuration = 0.5f;

        private static readonly Regex NumberHighlightRegex = new Regex(@"\d+(\.\d+)?%?", RegexOptions.Compiled);

        private readonly VisualTreeAsset _cardTemplate;
        private readonly VisualTreeAsset _slotItemTemplate;
        private readonly CompositeMotionHandle _cardMotions = new CompositeMotionHandle();

        private VisualElement _cardsContainer;
        private Label _levelCount;
        private List<VisualElement> _activeSlots;
        private List<VisualElement> _passiveSlots;

        /// <summary>
        /// SkillSelectView 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="root">오버레이의 최상위 VisualElement입니다.</param>
        /// <param name="cardTemplate">스킬 카드 UXML 템플릿입니다.</param>
        /// <param name="slotItemTemplate">로드아웃 슬롯에 채워질 SlotItem UXML 템플릿입니다.</param>
        public SkillSelectView(VisualElement root, VisualTreeAsset cardTemplate, VisualTreeAsset slotItemTemplate) : base(root)
        {
            _cardTemplate = cardTemplate;
            _slotItemTemplate = slotItemTemplate;
        }

        protected override void SetVisualElements()
        {
            _cardsContainer = _root.Q<VisualElement>("skill-select__cards");
            _levelCount = _root.Q<Label>(SelectLevelCount);
            _activeSlots = _root.Query<VisualElement>(className: ActiveSlotClass).ToList();
            _passiveSlots = _root.Query<VisualElement>(className: PassiveSlotClass).ToList();
        }

        protected override void RegisterCallbacks()
        {
            GameplayEvents.LevelChanged += OnLevelChanged;
        }

        private void OnLevelChanged(int level)
        {
            _levelCount.text = $"LV {level}";
        }

        /// <summary>
        /// 선택지 카드를 생성해 오버레이를 표시합니다.
        /// </summary>
        /// <param name="choices">노출할 선택지 목록</param>
        /// <param name="activeLoadout">보유 중인 액티브 스킬 목록</param>
        /// <param name="passiveLoadout">보유 중인 패시브 스킬 목록</param>
        public void ShowChoices(
            SkillChoice[] choices,
            IReadOnlyList<SkillLoadoutEntry> activeLoadout,
            IReadOnlyList<SkillLoadoutEntry> passiveLoadout)
        {
            RefreshLoadout(activeLoadout, passiveLoadout);
            ClearCards();

            for (int i = 0; i < choices.Length; i++)
            {
                _cardsContainer.Add(CreateCard(choices[i], i));
            }

            Show();
        }

        /// <summary>
        /// View가 생성한 카드의 트윈과 자식 요소를 정리합니다.
        /// </summary>
        public override void Dispose()
        {
            GameplayEvents.LevelChanged -= OnLevelChanged;
            ClearCards();
        }

        private void ClearCards()
        {
            _cardMotions.Cancel();
            _cardsContainer.Clear();
        }

        private VisualElement CreateCard(SkillChoice choice, int index)
        {
            TemplateContainer container = _cardTemplate.Instantiate();
            Button card = container.Q<Button>("skill-card");
            VisualElement background = card.Q<VisualElement>("skill-card__background");

            SkillData skill = choice.Skill;
            bool isActive = skill is ActiveSkillData;

            background.EnableInClassList(ActiveBackgroundModifier, isActive);
            background.EnableInClassList(PassiveBackgroundModifier, !isActive);

            UnityEngine.Sprite iconSprite = skill.Icon;

            if (iconSprite == null && skill is ActiveSkillData activeForIcon)
                iconSprite = activeForIcon.BallSprite;

            if (iconSprite != null)
                card.Q<VisualElement>("skill-card__icon").style.backgroundImage = new StyleBackground(iconSprite);

            card.Q<Label>("skill-card__name").text = skill.DisplayName;
            card.Q<Label>("skill-card__desc").text = HighlightNumbers(skill.GetDescription(choice.TargetLevel));

            SetEffectPreview(card, choice);
            SetLevelIndicators(card, choice.TargetLevel);
            RegisterCardInteractions(card, index);
            PlayRevealAnimation(card, background, index);

            return container;
        }

        private void SetEffectPreview(VisualElement card, SkillChoice choice)
        {
            VisualElement previewContainer = card.Q<VisualElement>("skill-card__effect-preview-container");

            if (!(choice.Skill is ActiveSkillData activeSkill))
            {
                previewContainer.style.display = DisplayStyle.None;
                return;
            }

            previewContainer.style.display = DisplayStyle.Flex;

            int currentValue = choice.IsUpgrade
                ? activeSkill.GetBallDamage(choice.TargetLevel - 1)
                : activeSkill.GetBallDamage(1);

            card.Q<Label>("skill-card__current-data-label").text = currentValue.ToString();

            VisualElement nextPreview = card.Q<VisualElement>("skill-card__next-data-preview");

            if (choice.IsUpgrade)
            {
                nextPreview.style.display = DisplayStyle.Flex;
                card.Q<Label>("skill-card__preview-data-label").text = activeSkill.GetBallDamage(choice.TargetLevel).ToString();
            }
            else
            {
                nextPreview.style.display = DisplayStyle.None;
            }
        }

        private void SetLevelIndicators(VisualElement card, int targetLevel)
        {
            List<VisualElement> indicators = card.Query<VisualElement>(className: LevelIndicatorClass).ToList();

            for (int i = 0; i < indicators.Count; i++)
            {
                int level = i + 1;
                indicators[i].style.display = level <= targetLevel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            VisualElement currentIndicator = indicators[targetLevel - 1];
            MotionHandle pulse = LMotion.Create(UnityEngine.Vector3.one, UnityEngine.Vector3.one * IndicatorPulseScale, IndicatorPulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToStyleScale(currentIndicator);
            _cardMotions.Add(pulse);
        }

        private void RegisterCardInteractions(Button card, int index)
        {
            int capturedIndex = index;
            card.clicked += () => GameUIEvents.SkillChoiceSelected?.Invoke(capturedIndex);
        }

        private void PlayRevealAnimation(VisualElement card, VisualElement background, int index)
        {
            background.style.height = 0f;

            long delay = CardRevealBaseDelayMs + index * CardRevealStaggerStepMs;

            background.schedule.Execute(() =>
            {
                float targetHeight = card.resolvedStyle.height;

                MotionHandle handle = LMotion.Create(0f, targetHeight, CardRevealDuration)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToStyleHeight(background);
                _cardMotions.Add(handle);
            }).ExecuteLater(delay);
        }

        private void RefreshLoadout(
            IReadOnlyList<SkillLoadoutEntry> activeLoadout,
            IReadOnlyList<SkillLoadoutEntry> passiveLoadout)
        {
            UpdateSlotGroup(_activeSlots, activeLoadout);
            UpdateSlotGroup(_passiveSlots, passiveLoadout);
        }

        private void UpdateSlotGroup(List<VisualElement> slots, IReadOnlyList<SkillLoadoutEntry> loadout)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                VisualElement slot = slots[i];
                slot.Clear();

                bool filled = loadout != null && i < loadout.Count;
                slot.EnableInClassList(FilledSlotModifier, filled);

                if (!filled)
                    continue;

                TemplateContainer slotItem = _slotItemTemplate.Instantiate();
                BindSlotItem(slotItem, loadout[i]);
                slot.Add(slotItem);
            }
        }

        private static void BindSlotItem(VisualElement slotItem, SkillLoadoutEntry entry)
        {
            slotItem.style.width = new StyleLength(Length.Percent(100f));
            slotItem.style.height = new StyleLength(Length.Percent(100f));
            
            UnityEngine.Sprite iconSprite = entry.Skill.Icon;

            if (iconSprite == null && entry.Skill is ActiveSkillData activeSkill)
                iconSprite = activeSkill.BallSprite;

            if (iconSprite != null)
                slotItem.Q<VisualElement>("slot-item__icon").style.backgroundImage = new StyleBackground(iconSprite);

            slotItem.Q<Label>("slot-item__count").text = $"<size=30>x</size>{entry.Level}";
        }

        private static string HighlightNumbers(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return NumberHighlightRegex.Replace(text, m => $"<color=#{HighlightColorHex}>{m.Value}</color>");
        }
    }
}
