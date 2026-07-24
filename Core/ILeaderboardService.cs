using System.Collections.Generic;
using System.Threading.Tasks;

namespace BounceHeroes.Core
{
    /// <summary>
    /// 글로벌 리더보드에 점수를 제출하고 순위를 조회하는 서비스입니다.
    /// 구현은 스왑 가능합니다(로컬 폴백 / PlayFab 등). 소비자는 이 인터페이스만 [Inject]로 주입받습니다.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>익명 로그인을 미리 수행합니다. 이미 로그인되어 있으면 즉시 반환합니다.</summary>
        Task LoginAsync();

        /// <summary>점수를 제출합니다. 보통 기존 최고점보다 높을 때만 반영됩니다.</summary>
        Task SubmitScoreAsync(long score);

        /// <summary>상위 <paramref name="count"/>명을 순위순으로 반환합니다.</summary>
        Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int count);

        /// <summary>본인의 리더보드 항목(순위·점수)을 반환합니다.</summary>
        Task<LeaderboardEntry> GetSelfAsync();
    }
}
