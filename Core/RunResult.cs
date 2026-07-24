namespace BounceHeroes.Core
{
    /// <summary>
    /// 한 판(런)이 끝났을 때의 최종 점수와 성과 통계를 담는 값입니다.
    /// 결과 화면 표시와 리더보드 제출에 사용됩니다.
    /// </summary>
    public readonly struct RunResult
    {
        /// <summary>최종 점수입니다.</summary>
        public readonly long Score;

        /// <summary>점수를 환산한 등급입니다.</summary>
        public readonly ScoreGrade Grade;

        /// <summary>승리 여부입니다.</summary>
        public readonly bool Won;

        /// <summary>이번 판이 기존 최고점을 넘었는지 여부입니다.</summary>
        public readonly bool IsNewRecord;

        /// <summary>총 처치 수입니다.</summary>
        public readonly int Kills;

        /// <summary>클리어한 웨이브 수입니다.</summary>
        public readonly int WavesCleared;

        /// <summary>이번 판의 최대 콤보입니다.</summary>
        public readonly int MaxCombo;

        /// <summary>치명타 적중 횟수입니다.</summary>
        public readonly int CritHits;

        /// <summary>발사한 볼의 총 개수입니다.</summary>
        public readonly int BallsFired;

        /// <summary>플레이 시간(초)입니다.</summary>
        public readonly float DurationSeconds;

        /// <summary>종료 시점의 잔여 체력입니다.</summary>
        public readonly int HpRemaining;

        /// <summary>최대 체력입니다.</summary>
        public readonly int HpMax;

        public RunResult(
            long score, ScoreGrade grade, bool won, bool isNewRecord,
            int kills, int wavesCleared, int maxCombo, int critHits, int ballsFired,
            float durationSeconds, int hpRemaining, int hpMax)
        {
            Score = score;
            Grade = grade;
            Won = won;
            IsNewRecord = isNewRecord;
            Kills = kills;
            WavesCleared = wavesCleared;
            MaxCombo = maxCombo;
            CritHits = critHits;
            BallsFired = ballsFired;
            DurationSeconds = durationSeconds;
            HpRemaining = hpRemaining;
            HpMax = hpMax;
        }

        /// <summary>IsNewRecord만 바꾼 복사본을 반환합니다. (최고점 판정은 저장을 담당하는 상위 계층이 세팅)</summary>
        public RunResult WithNewRecord(bool isNewRecord)
        {
            return new RunResult(
                Score, Grade, Won, isNewRecord,
                Kills, WavesCleared, MaxCombo, CritHits, BallsFired,
                DurationSeconds, HpRemaining, HpMax);
        }
    }
}
