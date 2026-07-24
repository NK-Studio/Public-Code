using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BounceHeroes.Core;
using UnityEngine;

namespace BounceHeroes.Leaderboard
{
    /// <summary>
    /// 기기 로컬에 점수를 저장하는 리더보드 폴백 구현입니다.
    /// PlayFab이 설정되지 않아도 리더보드 UI가 동작하도록, 시드 경쟁자 몇 명과 본인 최고점을 병합해 순위를 냅니다.
    /// (온라인 구현으로 교체되면 이 서비스는 사용되지 않습니다.)
    /// </summary>
    public sealed class LocalLeaderboardService : ILeaderboardService
    {
        private const string SelfBestKey = "Leaderboard.SelfBest";
        private const string NicknameKey = "Player.Nickname";
        private const string DefaultNickname = "나";

        // 보드가 비어 보이지 않도록 하는 데모용 시드 경쟁자입니다.
        private static readonly (string Name, long Score)[] SeedRivals =
        {
            ("별빛토끼", 18500),
            ("핀볼왕", 15200),
            ("통통이", 11000),
            ("슬라임헌터", 7600),
            ("여행자", 5100),
            ("뉴비", 3200),
        };

        public Task LoginAsync()
        {
            return Task.CompletedTask;
        }

        public Task SubmitScoreAsync(long score)
        {
            if (score > LoadSelfBest())
            {
                PlayerPrefs.SetString(SelfBestKey, score.ToString(System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int count)
        {
            List<LeaderboardEntry> ranked = BuildRankedBoard();
            if (count > 0 && ranked.Count > count)
                ranked = ranked.GetRange(0, count);

            return Task.FromResult<IReadOnlyList<LeaderboardEntry>>(ranked);
        }

        public Task<LeaderboardEntry> GetSelfAsync()
        {
            foreach (LeaderboardEntry entry in BuildRankedBoard())
                if (entry.IsSelf)
                    return Task.FromResult(entry);

            // 아직 점수가 없으면 맨 끝 순위로 표시합니다.
            return Task.FromResult(new LeaderboardEntry(SeedRivals.Length + 1, GetNickname(), LoadSelfBest(), true));
        }

        private List<LeaderboardEntry> BuildRankedBoard()
        {
            var rows = new List<(string Name, long Score, bool IsSelf)>();

            foreach ((string name, long score) in SeedRivals)
                rows.Add((name, score, false));

            rows.Add((GetNickname(), LoadSelfBest(), true));

            // 점수 내림차순 정렬(동점이면 본인을 위로).
            rows.Sort((a, b) =>
            {
                int cmp = b.Score.CompareTo(a.Score);
                if (cmp != 0)
                    return cmp;
                return b.IsSelf.CompareTo(a.IsSelf);
            });

            var result = new List<LeaderboardEntry>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
                result.Add(new LeaderboardEntry(i + 1, rows[i].Name, rows[i].Score, rows[i].IsSelf));

            return result;
        }

        private static long LoadSelfBest()
        {
            string raw = PlayerPrefs.GetString(SelfBestKey, "0");
            return long.TryParse(raw, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long value)
                ? value
                : 0L;
        }

        private static string GetNickname()
        {
            string nickname = PlayerPrefs.GetString(NicknameKey, DefaultNickname);
            return string.IsNullOrWhiteSpace(nickname) ? DefaultNickname : nickname;
        }
    }
}
