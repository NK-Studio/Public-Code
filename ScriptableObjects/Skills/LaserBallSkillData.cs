using BounceHeroes.Core;
using System.Collections.Generic;
using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 레이저 볼 스킬입니다. 타격한 몬스터와 같은 행의 모든 적에게 피해를 입힙니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_LaserBall", menuName = "BounceHeroes/Skills/Laser Ball")]
    public class LaserBallSkillData : ActiveSkillData
    {
        [Header("Laser")]
        [SerializeField] private int[] rowDamage = { 7, 11, 15 };

        /// <summary>
        /// 타격한 몬스터와 같은 행의 모든 적에게 레이저 피해를 입힙니다.
        /// </summary>
        /// <param name="context">타격 상황 정보</param>
        public override void OnMonsterHit(in SkillHitContext context)
        {
            int damage = rowDamage[context.Level - 1];
            float rowY = context.Target.transform.position.y;
            float halfBand = context.Grid.CellHeight * 0.5f;

            List<Monster> rowMonsters = context.Grid.GetMonstersInBand(rowY, halfBand);
            CombatEvents.RowLaserFired?.Invoke(rowY);

            foreach (Monster monster in rowMonsters)
            {
                if (!monster.IsDead)
                    monster.TakeEffectDamage(damage, true);
            }
        }
    }
}
