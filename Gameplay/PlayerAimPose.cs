using BounceHeroes.Data;
using BounceHeroes.Input;
using BounceHeroes.Managers;
using UnityEngine;
using VContainer;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 드래그 포인터 위치에 맞춰 플레이어의 좌우 방향과 조준 포즈를 갱신합니다.
    /// </summary>
    public sealed class PlayerAimPose : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform weapon;
        [SerializeField] private float headZRange = 12f;
        [SerializeField] private float weaponZRange = 35f;
        [SerializeField] private bool resetWhenReleased = true;

        private Camera _camera;
        private float _baseScaleX;
        private float _baseHeadZ;
        private float _baseWeaponZ;
        private IGameFlow _gameFlow;

        [Inject]
        public void Construct(IGameFlow gameFlow)
        {
            _gameFlow = gameFlow;
        }

        private void Awake()
        {
            _camera = Camera.main;
            _baseScaleX = Mathf.Abs(transform.localScale.x);

            if (head != null)
                _baseHeadZ = NormalizeAngle(head.localEulerAngles.z);

            if (weapon != null)
                _baseWeaponZ = NormalizeAngle(weapon.localEulerAngles.z);
        }

        private void Update()
        {
            if (_gameFlow != null && _gameFlow.State != GameState.Playing)
            {
                if (resetWhenReleased)
                    ApplyPose(0f);

                return;
            }

            if (!PointerInput.TryGetCurrent(out Vector2 pointerPosition, out bool isPressed) || !isPressed)
            {
                if (resetWhenReleased)
                    ApplyPose(0f);

                return;
            }

            Vector2 pointerWorld = _camera.ScreenToWorldPoint(pointerPosition);
            Vector2 direction = pointerWorld - (Vector2)transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            bool isFacingRight = direction.x > 0f;
            SetFacing(isFacingRight);

            Vector2 localDirection = direction.normalized;

            if (isFacingRight)
                localDirection.x *= -1f;

            float zOffset = Vector2.SignedAngle(Vector2.up, localDirection);
            ApplyPose(zOffset);
        }

        private void SetFacing(bool isFacingRight)
        {
            Vector3 localScale = transform.localScale;
            localScale.x = _baseScaleX * (isFacingRight ? -1f : 1f);
            transform.localScale = localScale;
        }

        private void ApplyPose(float zOffset)
        {
            if (head != null)
                SetLocalZ(head, _baseHeadZ + Mathf.Clamp(zOffset, -headZRange, headZRange));

            if (weapon != null)
                SetLocalZ(weapon, _baseWeaponZ + Mathf.Clamp(zOffset, -weaponZRange, weaponZRange));
        }

        private static void SetLocalZ(Transform target, float z)
        {
            Vector3 euler = target.localEulerAngles;
            euler.z = z;
            target.localEulerAngles = euler;
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
    }
}
