using System;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// UI 화면의 표시 상태와 VisualElement 바인딩을 관리하는 기본 View입니다.
    /// </summary>
    public class UIView : IDisposable
    {
        protected readonly VisualElement _root;

        /// <summary>View의 최상위 VisualElement를 반환합니다.</summary>
        public VisualElement Root => _root;

        /// <summary>View가 숨겨져 있는지 여부를 반환합니다.</summary>
        public bool IsHidden => _root.style.display == DisplayStyle.None;

        /// <summary>
        /// UIView 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="root">View의 최상위 VisualElement입니다.</param>
        public UIView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// View 요소와 콜백을 초기화합니다.
        /// </summary>
        public virtual void Initialize()
        {
            SetVisualElements();
            RegisterCallbacks();
            Hide();
        }

        protected virtual void SetVisualElements()
        {
        }

        protected virtual void RegisterCallbacks()
        {
        }

        /// <summary>
        /// View를 표시합니다.
        /// </summary>
        public virtual void Show()
        {
            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// View를 숨깁니다.
        /// </summary>
        public virtual void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// View가 등록한 콜백과 이벤트 구독을 해제합니다.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
