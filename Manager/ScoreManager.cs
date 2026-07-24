using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using BounceHeroes.Core;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 정적 이벤트 허브를 구독해 "언제 점수가 오르나"를 결정하고 <see cref="IScoreService"/>에 위임합니다.
    /// 런 종료 시 최종 <see cref="RunResult"/>를 확정하고 최고점수를 저장한 뒤 <see cref="GameplayEvents.RunCompleted"/>로 발행합니다.
    /// (실제 점수 계산은 서비스가 담당 — AudioManager/JuiceManager와 동일한 역할 분담)
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        private const string BestScoreKey = "Score.Best";
        private const int LeaderboardTopCount = 10;

        private IScoreService _score;
        private ILeaderboardService _leaderboard;
        private bool _runEnded;

        [Inject]
        public void Construct(IScoreService score, ILeaderboardService leaderboard)
        {
            _score = score;
            _leaderboard = leaderboard;
        }

        private void Start()
        {
            _runEnded = false;
            _score.BeginRun(Time.unscaledTime);
            GameplayEvents.ScoreChanged?.Invoke(_score.CurrentScore);
        }

        private void Update()
        {
            if (!_runEnded)
                _score.TickCombo(Time.unscaledTime);
        }

        private void OnEnable()
        {
            CombatEvents.MonsterKilled += OnMonsterKilled;
            CombatEvents.MonsterHitLanded += OnMonsterHitLanded;
            CombatEvents.BallFired += OnBallFired;
            CombatEvents.PlayerHit += OnPlayerHit;

            GameplayEvents.WaveChanged += OnWaveChanged;
            GameplayEvents.PlayerHpChanged += OnPlayerHpChanged;
            GameplayEvents.GameEnded += OnGameEnded;
        }

        private void OnDisable()
        {
            CombatEvents.MonsterKilled -= OnMonsterKilled;
            CombatEvents.MonsterHitLanded -= OnMonsterHitLanded;
            CombatEvents.BallFired -= OnBallFired;
            CombatEvents.PlayerHit -= OnPlayerHit;

            GameplayEvents.WaveChanged -= OnWaveChanged;
            GameplayEvents.PlayerHpChanged -= OnPlayerHpChanged;
            GameplayEvents.GameEnded -= OnGameEnded;
        }

        private void OnMonsterKilled(Vector3 position)
        {
            if (_runEnded)
                return;

            _score.RegisterKill(Time.unscaledTime);
            GameplayEvents.ScoreChanged?.Invoke(_score.CurrentScore);
        }

        private void OnMonsterHitLanded(Vector3 position, bool isCrit)
        {
            if (_runEnded)
                return;

            _score.RegisterHit(isCrit);
            if (isCrit)
                GameplayEvents.ScoreChanged?.Invoke(_score.CurrentScore);
        }

        private void OnBallFired(Vector3 position, Vector2 direction) => _score.RegisterBallFired();

        private void OnPlayerHit(Vector3 position) => _score.RegisterPlayerHit();

        private void OnWaveChanged(int current, int total)
        {
            _score.RegisterWaveReached(current, total);
            GameplayEvents.ScoreChanged?.Invoke(_score.CurrentScore);
        }

        private void OnPlayerHpChanged(int current, int max) => _score.UpdateHp(current, max);

        private void OnGameEnded(bool won)
        {
            if (_runEnded)
                return;

            _runEnded = true;

            RunResult result = _score.BuildResult(won, Time.unscaledTime);

            long best = LoadBestScore();
            bool isNewRecord = result.Score > best;
            if (isNewRecord)
                SaveBestScore(result.Score);

            // 결과 카드는 즉시 표시하고, 리더보드 제출/조회는 비동기로 진행해 준비되면 채운다.
            GameplayEvents.RunCompleted?.Invoke(result.WithNewRecord(isNewRecord));
            SubmitAndFetchLeaderboard(result.Score);
        }

        private async void SubmitAndFetchLeaderboard(long score)
        {
            if (_leaderboard == null)
                return;

            try
            {
                await _leaderboard.SubmitScoreAsync(score);
                IReadOnlyList<LeaderboardEntry> top = await _leaderboard.GetTopAsync(LeaderboardTopCount);
                LeaderboardEntry self = await _leaderboard.GetSelfAsync();
                GameplayEvents.LeaderboardReady?.Invoke(top, self);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScoreManager] 리더보드 처리 실패: {e.Message}");
            }
        }

        private static long LoadBestScore()
        {
            string raw = PlayerPrefs.GetString(BestScoreKey, "0");
            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? value
                : 0L;
        }

        private static void SaveBestScore(long score)
        {
            PlayerPrefs.SetString(BestScoreKey, score.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
