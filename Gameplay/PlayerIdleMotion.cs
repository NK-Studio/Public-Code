using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 장식용 Idle 캐릭터(IdleOnlyPlayer)에 "둠칫둠칫" 통통 튀는 느낌을 준다.
    /// <see cref="root"/>는 캐릭터의 발 밑 중앙에 위치한 피벗(Root 트랜스폼)이므로,
    /// 이 피벗의 스케일을 위아래로 눌렀다 펴면 발은 그대로 붙어 있고 몸통만 스쿼시&amp;스트레치된다.
    /// </summary>
    public sealed class PlayerIdleMotion : MonoBehaviour
    {
        [SerializeField, Tooltip("발 밑 중앙 피벗. 이 트랜스폼의 스케일을 애니메이션한다.")]
        private Transform root;

        [Header("Idle Motion")]
        [SerializeField] private float idleDuration = 0.55f;
        [SerializeField] private float idleSquashScaleY = 0.9f;
        [SerializeField] private float idleSquashScaleX = 1.06f;

        private Vector3 _rootBaseScale;
        private MotionHandle _idleTween;

        private void OnEnable()
        {
            if (root == null)
                return;

            _rootBaseScale = root.localScale;
            StartIdle();
        }

        private void OnDisable()
        {
            _idleTween.TryCancel();

            if (root != null)
                root.localScale = _rootBaseScale;
        }

        private void StartIdle()
        {
            root.localScale = _rootBaseScale;

            Vector3 squashTarget = new Vector3(
                _rootBaseScale.x * idleSquashScaleX,
                _rootBaseScale.y * idleSquashScaleY,
                _rootBaseScale.z);

            _idleTween = LMotion.Create(_rootBaseScale, squashTarget, idleDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .BindToLocalScale(root);
        }
    }
}
