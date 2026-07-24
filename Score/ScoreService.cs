using BounceHeroes.Core;
using BounceHeroes.Data;
using UnityEngine;

namespace BounceHeroes.Score
{
    /// <summary>
    /// 처치·콤보·치명타·웨이브 클리어·잔여 체력·클리어 시간을 점수로 환산하는 서비스입니다.
    /// 점수 상수와 등급 임계값은 난이도 스코프인 <see cref="GameBalanceData"/>에서 주입받습니다.
    /// (FXService와 동일하게 순수 C# + 생성자 주입)
    /// </summary>
    public sealed class ScoreService : IScoreService
    {
        private readonly GameBalanceData _balance;

        private long _score;
        private int _combo;
        private int _maxCombo;
        private int _kills;
        private int _critHits;
        private int _ballsFired;

        private int _totalWaves;
        private int _waveClearsAwarded;

        private int _hpCurrent;
        private int _hpMax = 1;

        private float _startTime;
        private float _lastKillTime;

        public ScoreService(GameBalanceData balance)
        {
            _balance = balance;
        }

        public long CurrentScore => _score;

        public int CurrentCombo => _combo;

        public void BeginRun(float now)
        {
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _kills = 0;
            _critHits = 0;
            _ballsFired = 0;
            _totalWaves = 0;
            _waveClearsAwarded = 0;
            _hpCurrent = _balance != null ? _balance.PlayerMaxHp : 1;
            _hpMax = _balance != null ? _balance.PlayerMaxHp : 1;
            _startTime = now;
            _lastKillTime = now;
        }

        public void TickCombo(float now)
        {
            if (_combo > 0 && now - _lastKillTime > _balance.ComboTimeWindow)
                _combo = 0;
        }

        public void RegisterKill(float now)
        {
            // 시간창을 넘겼으면 콤보를 끊고 다시 시작한다.
            if (now - _lastKillTime > _balance.ComboTimeWindow)
                _combo = 0;

            _combo++;
            if (_combo > _maxCombo)
                _maxCombo = _combo;

            float extra = Mathf.Min((_combo - 1) * _balance.ComboBonusPerStack,
                _balance.ComboMaxMultiplier - 1f);
            float multiplier = 1f + Mathf.Max(0f, extra);

            _score += Mathf.RoundToInt(_balance.ScorePerKill * multiplier);
            _kills++;
            _lastKillTime = now;
        }

        public void RegisterHit(bool isCrit)
        {
            if (!isCrit)
                return;

            _critHits++;
            _score += _balance.CritHitBonus;
        }

        public void RegisterBallFired()
        {
            _ballsFired++;
        }

        public void RegisterPlayerHit()
        {
            _combo = 0;
        }

        public void RegisterWaveReached(int current, int total)
        {
            _totalWaves = total;

            // WaveChanged는 웨이브 '시작' 시 호출되므로 current-1개가 방금 클리어된 것이다.
            int clearedSoFar = Mathf.Max(0, current - 1);
            int delta = clearedSoFar - _waveClearsAwarded;
            if (delta > 0)
            {
                _score += (long)delta * _balance.WaveClearBonus;
                _waveClearsAwarded = clearedSoFar;
            }
        }

        public void UpdateHp(int current, int max)
        {
            _hpCurrent = current;
            if (max > 0)
                _hpMax = max;
        }

        public RunResult BuildResult(bool won, float now)
        {
            long finalScore = _score;
            int wavesCleared = _waveClearsAwarded;
            float duration = Mathf.Max(0f, now - _startTime);

            if (won)
            {
                // 마지막 웨이브 클리어 보너스(다음 WaveChanged가 없어 미지급된 분)를 채운다.
                int remaining = Mathf.Max(0, _totalWaves - _waveClearsAwarded);
                finalScore += (long)remaining * _balance.WaveClearBonus;
                wavesCleared = _totalWaves;

                // 잔여 체력 보너스 + 빠른 클리어 시간 보너스는 생존(승리) 시에만 준다.
                float hpRatio = _hpMax > 0 ? Mathf.Clamp01((float)_hpCurrent / _hpMax) : 0f;
                finalScore += Mathf.RoundToInt(hpRatio * _balance.HpRemainingBonus);

                int timeBonus = Mathf.Max(0,
                    Mathf.RoundToInt(_balance.ClearTimeBonusMax - duration * _balance.ClearTimeBonusPerSecond));
                finalScore += timeBonus;
            }

            ScoreGrade grade = EvaluateGrade(finalScore);

            return new RunResult(
                finalScore, grade, won, false,
                _kills, wavesCleared, _maxCombo, _critHits, _ballsFired,
                duration, _hpCurrent, _hpMax);
        }

        private ScoreGrade EvaluateGrade(long score)
        {
            if (score >= _balance.GradeThresholdS)
                return ScoreGrade.S;
            if (score >= _balance.GradeThresholdA)
                return ScoreGrade.A;
            if (score >= _balance.GradeThresholdB)
                return ScoreGrade.B;
            if (score >= _balance.GradeThresholdC)
                return ScoreGrade.C;
            return ScoreGrade.D;
        }
    }
}
