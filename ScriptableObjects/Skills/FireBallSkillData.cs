using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 파이어 볼 스킬입니다. 타격 시 화상을 중첩시켜 초당 지속 피해를 입힙니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_FireBall", menuName = "BounceHeroes/Skills/Fire Ball")]
    public class FireBallSkillData : ActiveSkillData
    {
        [Header("Burn")]
        [SerializeField] private float[] burnDuration = { 4f, 4.5f, 5f };
        [SerializeField] private int[] maxStacks = { 3, 4, 5 };
        [SerializeField] private int[] tickDamage = { 8, 10, 12 };

        /// <summary>
        /// 타격한 몬스터에게 화상 스택을 적용합니다.
        /// </summary>
        /// <param name="context">타격 상황 정보</param>
        public override void OnMonsterHit(in SkillHitContext context)
        {
            int index = context.Level - 1;
            context.Target.ApplyBurn(burnDuration[index], maxStacks[index], tickDamage[index]);
        }
    }
}
