using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 아이스 볼 스킬입니다. 확률적으로 몬스터를 냉동시켜 이동을 늦추고 받는 피해를 증가시킵니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_IceBall", menuName = "BounceHeroes/Skills/Ice Ball")]
    public class IceBallSkillData : ActiveSkillData
    {
        [Header("Freeze")]
        [SerializeField] private float[] freezeChance = { 0.3f, 0.35f, 0.4f };
        [SerializeField] private float[] freezeDuration = { 5f, 6f, 7f };
        [SerializeField] private float[] slowPercent = { 0.1f, 0.15f, 0.2f };
        [SerializeField] private float[] bonusDamagePercent = { 0.1f, 0.15f, 0.2f };

        /// <summary>
        /// 확률 판정 후 타격한 몬스터에게 냉동 효과를 적용합니다.
        /// </summary>
        /// <param name="context">타격 상황 정보</param>
        public override void OnMonsterHit(in SkillHitContext context)
        {
            int index = context.Level - 1;

            if (Random.value > freezeChance[index])
                return;

            context.Target.ApplyFreeze(freezeDuration[index], slowPercent[index], bonusDamagePercent[index]);
        }
    }
}
