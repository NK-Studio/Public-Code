namespace BounceHeroes.Core
{
    /// <summary>
    /// 한 판(런) 동안의 점수·콤보·성과 통계를 누적하고 최종 <see cref="RunResult"/>를 계산하는 서비스입니다.
    /// "무엇이 점수인가"만 담당하며, "언제 점수가 오르나"는 이벤트 허브를 구독하는 상위 계층(ScoreManager)이 결정합니다.
    /// 시간이 필요한 계산은 <c>Time.unscaledTime</c>을 호출자가 넘겨줍니다(일시정지 중에도 일관되도록).
    /// </summary>
    public interface IScoreService
    {
        /// <summary>현재까지 누적된 점수입니다.</summary>
        long CurrentScore { get; }

        /// <summary>현재 콤보 수(연속 처치)입니다.</summary>
        int CurrentCombo { get; }

        /// <summary>새 판을 시작하며 모든 누적을 초기화합니다.</summary>
        /// <param name="now">현재 시각(Time.unscaledTime).</param>
        void BeginRun(float now);

        /// <summary>콤보 시간창 만료를 처리합니다. 매 프레임 호출됩니다.</summary>
        /// <param name="now">현재 시각(Time.unscaledTime).</param>
        void TickCombo(float now);

        /// <summary>몬스터 처치를 반영합니다(콤보 증가 + 점수 가산).</summary>
        /// <param name="now">현재 시각(Time.unscaledTime).</param>
        void RegisterKill(float now);

        /// <summary>타격을 반영합니다. 치명타면 보너스를 가산합니다.</summary>
        void RegisterHit(bool isCrit);

        /// <summary>볼 발사를 반영합니다.</summary>
        void RegisterBallFired();

        /// <summary>플레이어 피격을 반영합니다(콤보 리셋).</summary>
        void RegisterPlayerHit();

        /// <summary>도달한 웨이브를 반영합니다.</summary>
        /// <param name="current">현재 웨이브(1부터).</param>
        /// <param name="total">전체 웨이브 수.</param>
        void RegisterWaveReached(int current, int total);

        /// <summary>잔여 체력을 갱신합니다.</summary>
        void UpdateHp(int current, int max);

        /// <summary>현재까지의 누적으로 최종 결과를 계산합니다. (IsNewRecord는 호출자가 세팅)</summary>
        /// <param name="won">승리 여부.</param>
        /// <param name="now">현재 시각(Time.unscaledTime).</param>
        RunResult BuildResult(bool won, float now);
    }
}
