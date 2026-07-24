using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 글로벌 랭킹 화면입니다. Blur 없이, 화면이 전환된 것처럼 오른쪽에서 밀려 들어와 Home을 덮고
    /// 뒤로가기 버튼을 누르면 다시 오른쪽으로 밀려 나가며 닫힙니다.
    /// </summary>
    public sealed class RankingView : UIView
    {
        private const float SlideDuration = 0.32f;

        private readonly CompositeMotionHandle _motions = new CompositeMotionHandle();

        private Button _backButton;
        private ScrollView _list;

        /// <summary>뒤로가기 버튼을 눌렀을 때 발생합니다.</summary>
        public event Action BackRequested;

        public RankingView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _backButton = _root.Q<Button>("ranking__back-button");
            _list = _root.Q<ScrollView>("ranking__list");
        }

        protected override void RegisterCallbacks()
        {
            _backButton.clicked += OnBackClicked;
        }

        public override void Dispose()
        {
            _backButton.clicked -= OnBackClicked;
            _motions.Cancel();
        }

        /// <summary>화면이 오른쪽에서 밀려 들어오듯 랭킹 화면을 표시합니다.</summary>
        public override void Show()
        {
            _motions.Cancel();
            base.Show();

            _root.style.translate = new Translate(Length.Percent(100f), 0f);
            _motions.Add(
                LMotion.Create(new Vector2(100f, 0f), Vector2.zero, SlideDuration)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(_root, static (v, root) => root.style.translate = new Translate(Length.Percent(v.x), v.y)));
        }

        /// <summary>화면이 오른쪽으로 밀려 나가듯 랭킹 화면을 닫습니다. 이미 닫혀 있으면 애니메이션 없이 무시합니다.</summary>
        public override void Hide()
        {
            if (IsHidden)
                return;

            _motions.Cancel();

            _motions.Add(
                LMotion.Create(Vector2.zero, new Vector2(100f, 0f), SlideDuration)
                    .WithEase(Ease.InCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .WithOnComplete(() => base.Hide())
                    .Bind(_root, static (v, root) => root.style.translate = new Translate(Length.Percent(v.x), v.y)));
        }

        private void OnBackClicked() => BackRequested?.Invoke();

        /// <summary>상위 목록과 본인 항목으로 리스트를 채웁니다. 본인이 상위 목록에 없으면 구분선과 함께 덧붙입니다.</summary>
        public void SetEntries(IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry self)
        {
            _list.Clear();

            bool selfShown = false;
            if (top != null)
            {
                foreach (LeaderboardEntry entry in top)
                {
                    _list.Add(CreateRow(entry));
                    if (entry.IsSelf)
                        selfShown = true;
                }
            }

            if (!selfShown)
            {
                var gap = new Label("⋯");
                gap.style.fontSize = 20;
                gap.style.color = new Color(1f, 1f, 1f, 0.4f);
                gap.style.unityTextAlign = TextAnchor.MiddleCenter;
                _list.Add(gap);
                _list.Add(CreateRow(self));
            }
        }

        private VisualElement CreateRow(LeaderboardEntry entry)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingLeft = 20;
            row.style.paddingRight = 20;
            row.style.paddingTop = 14;
            row.style.paddingBottom = 14;
            row.style.marginBottom = 6;

            if (entry.IsSelf)
            {
                row.style.backgroundColor = new Color(1f, 0.85f, 0.2f, 0.18f);
                row.style.borderTopLeftRadius = 14;
                row.style.borderTopRightRadius = 14;
                row.style.borderBottomLeftRadius = 14;
                row.style.borderBottomRightRadius = 14;
            }

            Color textColor = entry.IsSelf ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 1f, 1f, 0.9f);

            var left = new Label($"#{entry.Rank}  {entry.Name}");
            left.style.fontSize = 30;
            left.style.color = textColor;

            var right = new Label(entry.Score.ToString("N0"));
            right.style.fontSize = 30;
            right.style.color = textColor;

            if (entry.IsSelf)
            {
                left.style.unityFontStyleAndWeight = FontStyle.Bold;
                right.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            row.Add(left);
            row.Add(right);
            return row;
        }
    }
}
