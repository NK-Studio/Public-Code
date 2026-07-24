using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using BounceHeroes.Data;
using UnityEngine;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 게임 로직에서 UI로 상태 변화를 전달하는 이벤트 허브입니다.
    /// </summary>
    public static class GameplayEvents
    {
        /// <summary>웨이브가 변경되었을 때 호출됩니다. (현재 웨이브, 전체 웨이브)</summary>
        public static Action<int, int> WaveChanged;

        /// <summary>플레이어 체력이 변경되었을 때 호출됩니다. (현재 체력, 최대 체력)</summary>
        public static Action<int, int> PlayerHpChanged;

        /// <summary>3택지 노출 조건인 처치 수가 변경되었을 때 호출됩니다. (현재 처치 수, 요구 처치 수)</summary>
        public static Action<int, int> KillProgressChanged;

        /// <summary>스테이지 전체 진행률이 변경되었을 때 호출됩니다. (사라진 몬스터 수, 스테이지 전체 몬스터 수)</summary>
        public static Action<int, int> StageProgressChanged;

        public static Action<int, int, int> LevelProgressChanged;

        /// <summary>플레이어 레벨이 변경되었을 때 호출됩니다. (새 레벨)</summary>
        public static Action<int> LevelChanged;

        /// <summary>3택지 선택지가 준비되었을 때 호출됩니다.</summary>
        public static Action<SkillChoice[]> SkillChoicesReady;

        /// <summary>3택지 선택을 마치고 플레이 상태로 복귀했을 때 호출됩니다.</summary>
        public static Action SelectionResumed;

        /// <summary>게임이 종료되었을 때 호출됩니다. (승리 여부)</summary>
        public static Action<bool> GameEnded;

        /// <summary>데미지 팝업 표시가 요청되었을 때 호출됩니다. (위치, 수치, 유형)</summary>
        public static Action<Vector3, int, DamagePopupType> DamagePopupRequested;

        /// <summary>스킬을 획득하거나 업그레이드했을 때 호출됩니다. (스킬, 레벨)</summary>
        public static Action<SkillData, int> SkillAcquired;

        public static Action<IReadOnlyList<SkillLoadoutEntry>, IReadOnlyList<SkillLoadoutEntry>> SkillLoadoutChanged;

        /// <summary>현재 점수가 변경되었을 때 호출됩니다. (누적 점수) HUD 실시간 표시에 사용됩니다.</summary>
        public static Action<long> ScoreChanged;

        /// <summary>한 판이 종료되어 최종 점수·통계가 확정되었을 때 호출됩니다. 결과 화면을 구동합니다.</summary>
        public static Action<RunResult> RunCompleted;

        /// <summary>점수 제출 후 리더보드 조회가 완료되었을 때 호출됩니다. (상위 목록, 본인 항목)</summary>
        public static Action<IReadOnlyList<LeaderboardEntry>, LeaderboardEntry> LeaderboardReady;
    }
}
