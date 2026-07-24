using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Utility
{
    [UxmlElement]
    public partial class SafeAreaElement : VisualElement
    {
        private bool _applyTop = true;
        private bool _applyBottom = true;
        private bool _applyLeft = true;
        private bool _applyRight = true;
        private bool _symmetricHorizontal = true;
        private bool _reserveAppliedInsets = false;
        private float _multiplier = 1f;
        private float _baseWidth = float.NaN;
        private VisualElement _contentContainer;
        private IVisualElementScheduledItem _safeAreaPoller;

        public override VisualElement contentContainer => _contentContainer;

        [UxmlAttribute]
        public bool ApplyTop
        {
            get => _applyTop;
            set
            {
                _applyTop = value;
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public bool ApplyBottom
        {
            get => _applyBottom;
            set
            {
                _applyBottom = value;
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public bool ApplyLeft
        {
            get => _applyLeft;
            set
            {
                _applyLeft = value;
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public bool ApplyRight
        {
            get => _applyRight;
            set
            {
                _applyRight = value;
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public bool SymmetricHorizontal
        {
            get => _symmetricHorizontal;
            set
            {
                _symmetricHorizontal = value;
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public bool ReserveAppliedInsets
        {
            get => _reserveAppliedInsets;
            set
            {
                _reserveAppliedInsets = value;
                ResetReservedSize(!_reserveAppliedInsets);
                ApplySafeArea();
            }
        }

        [UxmlAttribute]
        public float Multiplier
        {
            get => _multiplier;
            set
            {
                _multiplier = Mathf.Clamp01(value);
                ApplySafeArea();
            }
        }

        public SafeAreaElement()
        {
            _contentContainer = new VisualElement
            {
                name = "safe-area-content-container",
                pickingMode = PickingMode.Ignore
            };
            _contentContainer.style.flexGrow = 1;
            _contentContainer.style.flexShrink = 0;
            hierarchy.Add(_contentContainer);

            _safeAreaPoller = schedule.Execute(ApplySafeArea).Every(250);
            _safeAreaPoller.Pause();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(ApplySafeArea);
            _safeAreaPoller.Resume();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _safeAreaPoller.Pause();
            ClearSafeArea();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (panel == null)
                return;

            CopyContainerLayoutStyle();

            try
            {
                GetSafeAreaInsets(out float top, out float bottom, out float left, out float right);

                if (_symmetricHorizontal)
                {
                    float horizontal = Mathf.Max(left, right);
                    left = horizontal;
                    right = horizontal;
                }

                style.borderTopWidth = 0;
                style.borderBottomWidth = 0;
                style.borderLeftWidth = 0;
                style.borderRightWidth = 0;

                _contentContainer.style.marginTop = _applyTop ? top * _multiplier : 0;
                _contentContainer.style.marginBottom = _applyBottom ? bottom * _multiplier : 0;
                _contentContainer.style.marginLeft = _applyLeft ? left * _multiplier : 0;
                _contentContainer.style.marginRight = _applyRight ? right * _multiplier : 0;

                ApplyReservedSize(
                    _applyTop ? top * _multiplier : 0,
                    _applyBottom ? bottom * _multiplier : 0,
                    _applyLeft ? left * _multiplier : 0,
                    _applyRight ? right * _multiplier : 0);
            }
            catch (System.InvalidCastException)
            {
                ClearSafeArea();
            }
        }

        private void CopyContainerLayoutStyle()
        {
            _contentContainer.style.flexDirection = resolvedStyle.flexDirection;
            _contentContainer.style.justifyContent = resolvedStyle.justifyContent;
            _contentContainer.style.alignItems = resolvedStyle.alignItems;
            _contentContainer.style.alignContent = resolvedStyle.alignContent;
            _contentContainer.style.flexWrap = resolvedStyle.flexWrap;
        }

        private void ClearSafeArea()
        {
            if (_contentContainer == null)
                return;

            _contentContainer.style.marginTop = 0;
            _contentContainer.style.marginBottom = 0;
            _contentContainer.style.marginLeft = 0;
            _contentContainer.style.marginRight = 0;
            ResetReservedSize(true);
        }

        private void ApplyReservedSize(float top, float bottom, float left, float right)
        {
            if (!_reserveAppliedInsets)
                return;

            CacheBaseSize();

            if (!float.IsNaN(_baseWidth))
            {
                float reservedWidth = _baseWidth + left + right;
                style.width = reservedWidth;
                style.minWidth = reservedWidth;
            }
        }

        private void CacheBaseSize()
        {
            if (float.IsNaN(_baseWidth))
            {
                float width = resolvedStyle.width;
                if (width > 0)
                    _baseWidth = width;
            }

        }

        private void ResetReservedSize(bool clearInlineSize)
        {
            _baseWidth = float.NaN;

            if (!clearInlineSize)
                return;

            style.width = StyleKeyword.Null;
            style.minWidth = StyleKeyword.Null;
        }

        private void GetSafeAreaInsets(out float top, out float bottom, out float left, out float right)
        {
            Rect safeArea = Screen.safeArea;
            Vector2 leftTop = RuntimePanelUtils.ScreenToPanel(
                panel,
                new Vector2(safeArea.xMin, Screen.height - safeArea.yMax));
            Vector2 rightBottom = RuntimePanelUtils.ScreenToPanel(
                panel,
                new Vector2(Screen.width - safeArea.xMax, safeArea.yMin));

            top = leftTop.y;
            bottom = rightBottom.y;
            left = leftTop.x;
            right = rightBottom.x;
        }
    }
}
