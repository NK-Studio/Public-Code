using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 따뜻한 양철 심장 패시브입니다. 모든 노멀 볼의 추가 피해량을 증가시킵니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_TinHeart", menuName = "BounceHeroes/Skills/Tin Heart")]
    public class TinHeartSkillData : PassiveSkillData
    {
        [Header("Tin Heart")]
        [SerializeField] private float[] bonusPercent = { 0.2f, 0.3f, 0.4f };

        /// <summary>
        /// 노멀 볼 타격이면 퍼센트 보너스를 누적합니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        public override void ModifyDamage(ref DamageContext context, Monster target, int level)
        {
            if (context.IsNormalBall)
                context.PercentBonus += bonusPercent[level - 1];
        }
    }
}
