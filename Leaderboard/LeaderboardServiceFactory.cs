using BounceHeroes.Core;

namespace BounceHeroes.Leaderboard
{
    /// <summary>
    /// 스크립팅 디파인에 따라 알맞은 <see cref="ILeaderboardService"/> 구현을 생성합니다.
    /// DI 스코프(게임)와 Home 컨트롤러가 동일한 선택 로직을 공유하도록 한 곳에 모읍니다.
    /// </summary>
    public static class LeaderboardServiceFactory
    {
        public static ILeaderboardService Create()
        {
#if PLAYFAB_ENABLED
            return new PlayFabLeaderboardService();
#else
            return new LocalLeaderboardService();
#endif
        }
    }
}
