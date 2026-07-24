using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 사망 연출 전용 대역 비주얼입니다. 실제 플레이어와 달리 좌우 반전이나 조준 포즈 등
    /// 어떤 스크립트도 트랜스폼을 건드리지 않습니다. 좌우 방향은 트랜스폼 반전이 아니라
    /// Animator의 FacingRight 파라미터로 전달되어, 방향별로 미리 좌우 대칭 처리된 애니메이션
    /// (Dead / Dead_Left)을 선택 재생하므로 회전 애니메이션이 왜곡되지 않습니다.
    /// </summary>
    public sealed class PlayerDeathVisual : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip referenceClip;

        private static readonly int DeadTriggerHash = Animator.StringToHash("Dead");
        private static readonly int FacingRightHash = Animator.StringToHash("FacingRight");

        /// <summary>사망 애니메이션 클립의 길이(초)입니다. 결과 팝업 표시 타이밍 계산에 사용됩니다.</summary>
        public float ClipLength => referenceClip != null ? referenceClip.length : 0f;

        /// <summary>
        /// 지정한 위치에 배치하고, 사망 시점의 좌우 방향에 맞는 사망 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="worldPosition">실제 플레이어가 사망한 월드 위치입니다.</param>
        /// <param name="facingRight">사망 시점에 오른쪽을 보고 있었는지 여부입니다.</param>
        public void PlayAt(Vector3 worldPosition, bool facingRight)
        {
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;

            gameObject.SetActive(true);
            animator.SetBool(FacingRightHash, facingRight);
            animator.SetTrigger(DeadTriggerHash);
        }
    }
}
