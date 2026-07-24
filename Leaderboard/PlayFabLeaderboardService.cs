#if PLAYFAB_ENABLED
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BounceHeroes.Core;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ProgressionModels;
using UnityEngine;

namespace BounceHeroes.Leaderboard
{
    /// <summary>
    /// PlayFab 신형(Entity 기반) 리더보드 구현입니다. (스크립팅 디파인 <c>PLAYFAB_ENABLED</c> + PlayFab SDK 필요)
    /// LoginWithCustomID(익명, Entity 토큰 발급) → UpdateStatistics(HighScore) → GetLeaderboard / GetLeaderboardAroundEntity.
    /// PlayFab 콘솔에서 이 이름과 동일한 Statistic definition과, 그 Statistic을 연결한(Linked statistic)
    /// Leaderboard definition을 미리 만들어야 합니다.
    /// 콜백 기반 API를 <see cref="TaskCompletionSource{T}"/>로 감싸 Task API로 노출합니다.
    /// </summary>
    public sealed class PlayFabLeaderboardService : ILeaderboardService
    {
        private const string StatisticName = "HighScore";
        private const string LeaderboardName = "HighScoreLeaderboard";
        private const string CustomIdKey = "Player.PlayFabId";
        private const string NicknameKey = "Player.Nickname";
        private const string DefaultNickname = "나";

        private string _entityId;
        private Task _loginTask;

        public Task LoginAsync()
        {
            return EnsureLoggedInAsync();
        }

        public async Task SubmitScoreAsync(long score)
        {
            await EnsureLoggedInAsync();
            await UpdateDisplayNameAsync(GetNickname());

            var request = new UpdateStatisticsRequest
            {
                Statistics = new List<PlayFab.ProgressionModels.StatisticUpdate>
                {
                    new PlayFab.ProgressionModels.StatisticUpdate
                    {
                        Name = StatisticName,
                        Scores = new List<string> { Math.Max(score, 0).ToString() }
                    }
                }
            };

            var tcs = new TaskCompletionSource<bool>();
            PlayFabProgressionAPI.UpdateStatistics(request, _ => tcs.TrySetResult(true), err => tcs.TrySetException(ToException(err)));
            await tcs.Task;
        }

        public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int count)
        {
            await EnsureLoggedInAsync();

            var request = new GetEntityLeaderboardRequest
            {
                LeaderboardName = LeaderboardName,
                StartingPosition = 1,
                PageSize = (uint)Mathf.Clamp(count, 1, 100)
            };

            var tcs = new TaskCompletionSource<GetEntityLeaderboardResponse>();
            PlayFabProgressionAPI.GetLeaderboard(request, result => tcs.TrySetResult(result), err => tcs.TrySetException(ToException(err)));
            GetEntityLeaderboardResponse result = await tcs.Task;

            var entries = new List<LeaderboardEntry>(result.Rankings.Count);
            foreach (EntityLeaderboardEntry row in result.Rankings)
                entries.Add(ToEntry(row));

            return entries;
        }

        public async Task<LeaderboardEntry> GetSelfAsync()
        {
            await EnsureLoggedInAsync();

            var request = new GetLeaderboardAroundEntityRequest
            {
                LeaderboardName = LeaderboardName,
                MaxSurroundingEntries = 1
            };

            var tcs = new TaskCompletionSource<GetEntityLeaderboardResponse>();
            PlayFabProgressionAPI.GetLeaderboardAroundEntity(request, result => tcs.TrySetResult(result), err => tcs.TrySetException(ToException(err)));
            GetEntityLeaderboardResponse result = await tcs.Task;

            if (result.Rankings != null)
            {
                foreach (EntityLeaderboardEntry row in result.Rankings)
                {
                    if (row.Entity != null && row.Entity.Id == _entityId)
                        return ToEntry(row);
                }
            }

            return new LeaderboardEntry(0, GetNickname(), 0, true);
        }

        private Task EnsureLoggedInAsync()
        {
            if (_loginTask != null)
                return _loginTask;

            var request = new LoginWithCustomIDRequest
            {
                CustomId = GetOrCreateCustomId(),
                CreateAccount = true
            };

            var tcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.LoginWithCustomID(request,
                result =>
                {
                    _entityId = result.EntityToken?.Entity?.Id;
                    tcs.TrySetResult(true);
                },
                err => tcs.TrySetException(ToException(err)));

            _loginTask = tcs.Task;
            return _loginTask;
        }

        private Task UpdateDisplayNameAsync(string nickname)
        {
            var tcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.UpdateUserTitleDisplayName(
                new UpdateUserTitleDisplayNameRequest { DisplayName = nickname },
                _ => tcs.TrySetResult(true),
                _ => tcs.TrySetResult(false)); // 표시 이름 실패는 치명적이지 않으므로 무시합니다.
            return tcs.Task;
        }

        private LeaderboardEntry ToEntry(EntityLeaderboardEntry row)
        {
            string name = string.IsNullOrWhiteSpace(row.DisplayName) ? "익명" : row.DisplayName;
            bool isSelf = !string.IsNullOrEmpty(_entityId) && row.Entity != null && row.Entity.Id == _entityId;
            long score = row.Scores != null && row.Scores.Count > 0 && long.TryParse(row.Scores[0], out long parsed) ? parsed : 0;
            return new LeaderboardEntry(row.Rank + 1, name, score, isSelf);
        }

        private static Exception ToException(PlayFabError error)
        {
            return new Exception(error != null ? error.GenerateErrorReport() : "PlayFab error");
        }

        private static string GetOrCreateCustomId()
        {
            string id = PlayerPrefs.GetString(CustomIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(CustomIdKey, id);
                PlayerPrefs.Save();
            }

            return id;
        }

        private static string GetNickname()
        {
            string nickname = PlayerPrefs.GetString(NicknameKey, DefaultNickname);
            return string.IsNullOrWhiteSpace(nickname) ? DefaultNickname : nickname;
        }
    }
}
#endif
