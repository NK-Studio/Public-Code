using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 액티브 스킬 볼이 몬스터를 타격했을 때 스킬 효과 실행에 필요한 정보 묶음입니다.
    /// </summary>
    public struct SkillHitContext
    {
        /// <summary>타격당한 몬스터입니다.</summary>
        public Monster Target;

        /// <summary>타격 지점(월드 좌표)입니다.</summary>
        public Vector2 HitPoint;

        /// <summary>타격한 볼의 스킬 레벨입니다.</summary>
        public int Level;

        /// <summary>격자 필드 참조입니다. (레이저 볼의 행 조회 등)</summary>
        public GridField Grid;

        /// <summary>볼 발사기 참조입니다. (클러스터 볼의 소형 볼 생성 등)</summary>
        public BallLauncher Launcher;
    }
}
