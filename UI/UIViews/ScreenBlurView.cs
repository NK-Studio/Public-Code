using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.UI.UIViews
{
    public sealed class ScreenBlurView : UIView
    {
        private readonly Camera _sourceCamera;

        private RenderTexture _renderTexture;
        private int _renderTextureWidth;
        private int _renderTextureHeight;

        public ScreenBlurView(
            VisualElement rootElement,
            Camera sourceCamera) : base(rootElement)
        {
            _sourceCamera = sourceCamera;
        }

        public override void Show()
        {
            Capture();
            Root.style.display = DisplayStyle.Flex;
        }

        public override void Hide()
        {
            Root.style.display = DisplayStyle.None;
        }

        public override void Dispose()
        {
            base.Dispose();
            ReleaseRenderTexture();
        }

        private void Capture()
        {
            Camera camera = _sourceCamera != null ? _sourceCamera : Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[ScreenBlurView] 캡처할 카메라가 없습니다.");
                return;
            }

            EnsureRenderTexture();

            RenderTexture previousTargetTexture = camera.targetTexture;
            bool previousEnabled = camera.enabled;

            camera.enabled = false;
            camera.targetTexture = _renderTexture;
            camera.Render();
            camera.targetTexture = previousTargetTexture;
            camera.enabled = previousEnabled;

            Root.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
        }

        private void EnsureRenderTexture()
        {
            int targetWidth = Mathf.Max(1, Mathf.CeilToInt(GetPanelWidth()));
            int targetHeight = Mathf.Max(1, Mathf.CeilToInt(GetPanelHeight()));

            if (_renderTexture != null &&
                _renderTextureWidth == targetWidth &&
                _renderTextureHeight == targetHeight)
            {
                return;
            }

            ReleaseRenderTexture();

            _renderTextureWidth = targetWidth;
            _renderTextureHeight = targetHeight;

            _renderTexture = new RenderTexture(
                _renderTextureWidth,
                _renderTextureHeight,
                16,
                RenderTextureFormat.ARGB32)
            {
                name = "Runtime Screen Blur Capture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };

            _renderTexture.Create();
        }

        private float GetPanelWidth()
        {
            return Root.resolvedStyle.width > 0f
                ? Root.resolvedStyle.width
                : Screen.width;
        }

        private float GetPanelHeight()
        {
            return Root.resolvedStyle.height > 0f
                ? Root.resolvedStyle.height
                : Screen.height;
        }

        private void ReleaseRenderTexture()
        {
            if (_renderTexture == null)
                return;

            _renderTexture.Release();
            Object.Destroy(_renderTexture);

            _renderTexture = null;
            _renderTextureWidth = 0;
            _renderTextureHeight = 0;
        }
    }
}