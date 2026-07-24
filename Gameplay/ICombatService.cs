using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 볼 타격 한 건을 처리하는 데미지 파이프라인의 추상화입니다.
    /// </summary>
    public interface ICombatService
    {
        /// <summary>볼과 몬스터의 타격을 처리합니다.</summary>
        /// <param name="ball">타격한 볼</param>
        /// <param name="monster">타격당한 몬스터</param>
        /// <param name="contactNormal">물리 충돌 법선. 트리거(고스트 볼) 충돌이면 null</param>
        /// <param name="hitPoint">타격 지점(월드 좌표)</param>
        void ResolveHit(Ball ball, Monster monster, Vector2? contactNormal, Vector2 hitPoint);
    }
}
