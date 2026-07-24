using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 에메랄드 단검 패시브입니다. 적 후면 타격 시 치명타 확률이 증가합니다. (적마다 1회)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_EmeraldDagger", menuName = "BounceHeroes/Skills/Emerald Dagger")]
    public class EmeraldDaggerSkillData : PassiveSkillData
    {
        [Header("Emerald Dagger")]
        [SerializeField] private float[] critChanceBonus = { 0.2f, 0.3f, 0.4f };

        /// <summary>
        /// 후면 타격이고 해당 몬스터에게 아직 적용되지 않았다면 치명타 확률을 증가시킵니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        public override void ModifyDamage(ref DamageContext context, Monster target, int level)
        {
            if (context.Face != HitFace.Back || target.EmeraldConsumed)
                return;

            context.CritChance += critChanceBonus[level - 1];
            target.MarkEmeraldConsumed();
        }
    }
}
