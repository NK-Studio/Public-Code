namespace BounceHeroes.Core
{
    /// <summary>
    /// 리더보드 한 줄입니다. 순위·이름·점수와, 본인 항목 여부를 담습니다.
    /// </summary>
    public readonly struct LeaderboardEntry
    {
        /// <summary>1부터 시작하는 순위입니다.</summary>
        public readonly int Rank;

        /// <summary>표시 이름(닉네임)입니다.</summary>
        public readonly string Name;

        /// <summary>점수입니다.</summary>
        public readonly long Score;

        /// <summary>이 항목이 본인인지 여부입니다.</summary>
        public readonly bool IsSelf;

        public LeaderboardEntry(int rank, string name, long score, bool isSelf)
        {
            Rank = rank;
            Name = name;
            Score = score;
            IsSelf = isSelf;
        }
    }
}
