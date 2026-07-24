using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 고스트 볼 스킬입니다. 충돌 시 적을 관통하여 지나갑니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_GhostBall", menuName = "BounceHeroes/Skills/Ghost Ball")]
    public class GhostBallSkillData : ActiveSkillData
    {
        /// <summary>고스트 볼은 몬스터와 물리적으로 충돌하지 않고 관통합니다.</summary>
        public override bool ReflectsOffMonsters => false;
    }
}
