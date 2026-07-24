using UnityEngine;

namespace Utility
{
    /// <summary>
    /// 정사각형 POT(Power-of-Two) 텍스처로 납품된 세로로 긴 배경 아트를 원래 비율로 복원합니다.
    /// 모바일 배경은 세로로 긴 원본을 그대로 담으면 텍스처 메모리가 커지므로(POT 2048x4096 등),
    /// 아트 파이프라인에서 가로로 눌러 2048x2048 정사각형에 담아 압축률을 높이는 경우가 있습니다.
    /// 이 컴포넌트는 그 압축을 역산한 스케일/오프셋 값을 트랜스폼에 적용해 원래 그리드 비율로 되돌립니다.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SquishedBackgroundUnsquisher : MonoBehaviour
    {
        [Header("실제 기기 스크린샷과 대조하여 역산한 언스퀴시 값")]
        [SerializeField] private Vector3 unsquishScale = new Vector3(0.7012339f, 1.145605f, 0.9710833f);
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 0.687f, 0f);

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        /// <summary>
        /// 저장된 언스퀴시 스케일과 위치를 트랜스폼에 강제 적용합니다.
        /// 씬에서 실수로 트랜스폼이 리셋되어도 이 컴포넌트가 다시 값을 복원합니다.
        /// </summary>
        public void Apply()
        {
            transform.localScale = unsquishScale;
            transform.localPosition = localPosition;
        }
    }
}
