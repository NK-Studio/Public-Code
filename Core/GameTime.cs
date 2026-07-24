using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BounceHeroes.Core
{
    /// <summary>
    /// <see cref="Time.timeScale"/> 변경을 이 클래스 한 곳에서만 담당합니다.
    /// 선택 UI 정지, 결과 화면 정지, 히트스톱, 슬로모션이 각자 timeScale을 직접 대입하면
    /// 서로 덮어쓰면서 특정 상황(웨이브 클리어와 3택지 노출이 겹치는 등)에 0에서 복원되지
    /// 않고 멈추는 문제가 생길 수 있어, 아래 두 규칙으로 모든 변경을 통제합니다.
    ///
    /// - '완전 정지'(선택 UI, 결과 화면)는 참조 카운트로 관리되며 항상 최우선으로 적용된다.
    /// - '일시 효과'(히트스톱, 슬로모션)는 완전 정지 중이 아닐 때만 실행되고,
    ///   새 효과가 시작되면 이전 효과를 먼저 정리하며, 끝나면 항상 1로 복귀한다.
    /// </summary>
    public static class GameTime
    {
        private static MonoBehaviour _runner;
        private static int _pauseRefCount;
        private static CancellationTokenSource _effectCts;

        /// <summary>완전 정지 요청이 하나 이상 걸려 있는지 여부입니다.</summary>
        public static bool IsPaused => _pauseRefCount > 0;

        /// <summary>
        /// 코루틴을 실행할 MonoBehaviour를 등록하고 모든 정지 상태를 초기화합니다.
        /// 씬이 시작될 때마다(재시작 포함) GameManager가 한 번 호출해야 합니다.
        /// </summary>
        /// <param name="runner">효과 코루틴 실행에 사용할 MonoBehaviour</param>
        public static void Bind(MonoBehaviour runner)
        {
            _runner = runner;
            _pauseRefCount = 0;
            CancelEffect();
            Time.timeScale = 1f;
        }

        /// <summary>
        /// 완전 정지를 요청합니다. 선택 UI, 결과 화면 등 게임 진행을 멈춰야 할 때 사용합니다.
        /// 중첩 호출이 가능하며 참조 카운트로 관리되어, 마지막 Resume까지는 항상 정지 상태를 유지합니다.
        /// </summary>
        public static void Pause()
        {
            _pauseRefCount++;
            CancelEffect();
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Pause() 호출 하나를 해제합니다. 모든 정지 요청이 해제되어야 timeScale이 1로 복귀합니다.
        /// </summary>
        public static void Resume()
        {
            _pauseRefCount = Mathf.Max(0, _pauseRefCount - 1);

            if (_pauseRefCount == 0)
                Time.timeScale = 1f;
        }

        /// <summary>
        /// 완전 정지 상태가 아닐 때, 실시간 기준으로 짧게 timeScale을 낮췄다가 복원합니다.
        /// 히트스톱과 슬로모션 모두 이 메서드로 처리합니다.
        /// </summary>
        /// <param name="scale">지속 시간 동안 유지할 timeScale 값</param>
        /// <param name="realtimeDuration">실시간(Time.timeScale의 영향을 받지 않는) 기준 지속 시간(초)</param>
        public static void PlayEffect(float scale, float realtimeDuration)
        {
            if (IsPaused || _runner == null)
                return;

            CancelEffect();
            Time.timeScale = scale;

            // 러너가 파괴되면(씬 전환 등) 자동으로 취소되도록 러너의 파괴 토큰과 연결한다.
            _effectCts = CancellationTokenSource.CreateLinkedTokenSource(_runner.destroyCancellationToken);
            EffectAsync(realtimeDuration, _effectCts.Token).Forget();
        }

        private static async UniTaskVoid EffectAsync(float realtimeDuration, CancellationToken token)
        {
            // WaitForSecondsRealtime와 동일하게 timeScale의 영향을 받지 않는 실시간 지연이다.
            await UniTask.Delay(TimeSpan.FromSeconds(realtimeDuration),
                DelayType.UnscaledDeltaTime, cancellationToken: token);

            _effectCts?.Dispose();
            _effectCts = null;

            if (!IsPaused)
                Time.timeScale = 1f;
        }

        private static void CancelEffect()
        {
            if (_effectCts == null)
                return;

            _effectCts.Cancel();
            _effectCts.Dispose();
            _effectCts = null;
        }
    }
}
