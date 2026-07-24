using UnityEngine;

namespace BounceHeroes.Input
{
    /// <summary>스와이프 판정 결과 방향입니다.</summary>
    public enum SwipeDirection
    {
        None,
        Up,
        Down
    }

    /// <summary>
    /// 포인터의 press~release 좌표/시간으로 수직 스와이프(위/아래)를 판정하는 순수 로직입니다.
    /// rest-youth 프로젝트의 SwipeUpDetector를 <see cref="PointerInput"/> 기반 입력에 맞춰 이식했습니다.
    /// MonoBehaviour가 아니라 값 객체라, 별도 씬 배선 없이 기존 입력 처리 코드 안에서 바로 생성해 쓸 수 있습니다.
    /// </summary>
    public sealed class SwipeDetector
    {
        private readonly float _minDistance;
        private readonly float _maxTime;
        private readonly float _verticalThreshold;

        private Vector2 _startPosition;
        private float _startTime;

        public SwipeDetector(float minDistance, float maxTime, float verticalThreshold)
        {
            _minDistance = Mathf.Max(0f, minDistance);
            _maxTime = Mathf.Max(0f, maxTime);
            _verticalThreshold = Mathf.Clamp(verticalThreshold, 0.5f, 1f);
        }

        /// <summary>포인터가 눌린 시점의 화면 좌표로 추적을 시작합니다.</summary>
        public void BeginTrack(Vector2 startPosition)
        {
            _startPosition = startPosition;
            _startTime = Time.unscaledTime;
        }

        /// <summary>
        /// 포인터가 떼진 시점의 화면 좌표를 받아 스와이프 방향을 판정합니다.
        /// 제한 시간을 넘겼거나, 이동 거리가 부족하거나, 수직 성분이 충분하지 않으면 <see cref="SwipeDirection.None"/>입니다.
        /// </summary>
        public SwipeDirection EndTrack(Vector2 endPosition)
        {
            if (Time.unscaledTime - _startTime > _maxTime)
                return SwipeDirection.None;

            Vector2 delta = endPosition - _startPosition;

            if (delta.magnitude < _minDistance)
                return SwipeDirection.None;

            Vector2 direction = delta.normalized;

            if (Mathf.Abs(direction.y) < _verticalThreshold)
                return SwipeDirection.None;

            return direction.y > 0f ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
