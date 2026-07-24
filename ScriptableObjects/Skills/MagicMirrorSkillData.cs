using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 마법 거울 패시브입니다. 볼이 벽에 튕길 때마다 다음 타격 피해량이 증가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_MagicMirror", menuName = "BounceHeroes/Skills/Magic Mirror")]
    public class MagicMirrorSkillData : PassiveSkillData
    {
        [Header("Magic Mirror")]
        [SerializeField] private float[] perBouncePercent = { 0.2f, 0.4f, 0.6f };

        /// <summary>
        /// 누적된 벽 반사 횟수만큼 퍼센트 보너스를 누적합니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        public override void ModifyDamage(ref DamageContext context, Monster target, int level)
        {
            if (context.WallBounceStacks > 0)
                context.PercentBonus += perBouncePercent[level - 1] * context.WallBounceStacks;
        }
    }
}
