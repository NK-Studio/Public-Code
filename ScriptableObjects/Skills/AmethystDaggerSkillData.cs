using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 자수정 단검 패시브입니다. 적 전면 타격 시 치명타 확률이 증가합니다. (적마다 1회)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_AmethystDagger", menuName = "BounceHeroes/Skills/Amethyst Dagger")]
    public class AmethystDaggerSkillData : PassiveSkillData
    {
        [Header("Amethyst Dagger")]
        [SerializeField] private float[] critChanceBonus = { 0.1f, 0.2f, 0.3f };

        /// <summary>
        /// 전면 타격이고 해당 몬스터에게 아직 적용되지 않았다면 치명타 확률을 증가시킵니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        public override void ModifyDamage(ref DamageContext context, Monster target, int level)
        {
            if (context.Face != HitFace.Front || target.AmethystConsumed)
                return;

            context.CritChance += critChanceBonus[level - 1];
            target.MarkAmethystConsumed();
        }
    }
}
