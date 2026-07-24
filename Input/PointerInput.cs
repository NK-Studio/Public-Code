using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BounceHeroes.Input
{
    /// <summary>
    /// 터치스크린과 마우스 두 장치만 직접 조회해 현재 포인터 좌표/누름 상태를 반환합니다.
    /// <see cref="UnityEngine.InputSystem.Pointer"/>(모든 포인터 장치를 아우르는 상위 추상화) 대신,
    /// 이 프로젝트가 지원하는 장치만 명시적으로 우선순위를 두고 조회합니다.
    /// </summary>
    public static class PointerInput
    {
        /// <summary>
        /// 현재 포인터 좌표와 누름 상태를 반환합니다. 터치가 이번 프레임에 눌려있거나 막 떼어졌다면
        /// 터치를 우선하고(뗀 프레임의 좌표를 놓치지 않기 위해), 그 외에는 마우스를 사용합니다.
        /// 두 장치 모두 없으면 false를 반환합니다.
        /// </summary>
        public static bool TryGetCurrent(out Vector2 position, out bool isPressed)
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null)
            {
                TouchControl touch = touchscreen.primaryTouch;
                bool touchActive = touch.press.isPressed || touch.press.wasReleasedThisFrame;

                if (touchActive)
                {
                    position = touch.position.ReadValue();
                    isPressed = touch.press.isPressed;
                    return true;
                }
            }

            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                isPressed = mouse.leftButton.isPressed;
                return true;
            }

            position = default;
            isPressed = false;
            return false;
        }
    }
}
