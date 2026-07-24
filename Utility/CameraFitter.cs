using BounceHeroes.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// 화면 종횡비와 무관하게 "최소로 보여야 할 월드 영역"을 항상 담도록(contain) 카메라 크기를 맞춥니다.
    ///
    /// 종횡비별로 매직 넘버를 넣는 대신, 최소 가로 폭과 최소 세로 높이 두 값만 정의합니다.
    /// - 세로로 긴 바형 화면(폰)에서는 '가로 폭 확보'가 구속 조건이 되어 그 폭이 항상 보입니다.
    /// - 상대적으로 덜 긴 패드형 화면에서는 '세로 높이 확보'가 구속 조건이 되어 더 넓게 보입니다.
    ///
    /// orthographicSize = max(minHeight/2, (minWidth/2) / aspect)
    ///
    /// 예: minWidth=10, minHeight=18.66 일 때
    ///   - 폰(aspect 0.461) → size 10.85 → 보이는 halfWidth 5
    ///   - 패드(aspect 0.75) → size 9.33 → 보이는 halfWidth 7
    /// </summary>
    [ExecuteAlways]
    public sealed class CameraFitter : MonoBehaviour, IAutoBindable
    {
        [Header("항상 보여야 하는 최소 월드 영역")]
        [SerializeField] private float minVisibleWidth = 10f;
        [SerializeField] private float minVisibleHeight = 18.66f;

        [Header("대상 (둘 중 있는 것을 사용)")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Camera targetCamera;

        private float _lastAspect = -1f;

        private void OnEnable()
        {
            Fit(force: true);
        }

        /// <summary>
        /// 같은 게임오브젝트의 카메라 대상을 찾아 비어 있는 참조만 채웁니다. 에디터 "Auto Bind"에서 호출됩니다.
        /// </summary>
        public void AutoBind()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = GetComponent<CinemachineCamera>();

            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            Fit(force: false);
        }

        /// <summary>
        /// 현재 종횡비에 맞춰 카메라 직교 크기를 다시 계산해 적용합니다.
        /// </summary>
        /// <param name="force">종횡비 변화가 없어도 강제로 적용할지 여부</param>
        public void Fit(bool force)
        {
            float aspect = GetAspect();

            if (aspect <= 0f)
                return;

            if (!force && Mathf.Approximately(aspect, _lastAspect))
                return;

            _lastAspect = aspect;

            float sizeForHeight = minVisibleHeight * 0.5f;
            float sizeForWidth = (minVisibleWidth * 0.5f) / aspect;
            float size = Mathf.Max(sizeForHeight, sizeForWidth);

            ApplySize(size);
        }

        private float GetAspect()
        {
            if (targetCamera != null && targetCamera.aspect > 0f)
                return targetCamera.aspect;

            if (Screen.height > 0)
                return (float)Screen.width / Screen.height;

            return -1f;
        }

        private void ApplySize(float size)
        {
            // Cinemachine이 있으면 렌즈만 구동한다. (Brain이 실제 카메라 크기를 렌즈에서 가져감)
            if (cinemachineCamera != null)
            {
                LensSettings lens = cinemachineCamera.Lens;
                lens.OrthographicSize = size;
                cinemachineCamera.Lens = lens;
                return;
            }

            if (targetCamera != null)
                targetCamera.orthographicSize = size;
        }
    }
}
