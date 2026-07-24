using BounceHeroes.Core;
using System.Collections.Generic;
using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 마지막 성냥 패시브입니다. 적 사망 시 폭발하여 근처 적에게 피해를 입힙니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_LastMatch", menuName = "BounceHeroes/Skills/Last Match")]
    public class LastMatchSkillData : PassiveSkillData
    {
        [Header("Last Match")]
        [SerializeField] private int[] explosionDamage = { 10, 20, 30 };
        [SerializeField] private float explosionRadius = 1.6f;

        /// <summary>
        /// 처치된 몬스터 위치에서 폭발을 일으켜 근처 적에게 피해를 입힙니다.
        /// </summary>
        /// <param name="target">처치된 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        /// <param name="grid">격자 필드 참조</param>
        public override void OnMonsterKilled(Monster target, int level, GridField grid)
        {
            int damage = explosionDamage[level - 1];
            Vector3 center = target.transform.position;

            CombatEvents.ExplosionFired?.Invoke(center, explosionRadius);

            List<Monster> nearby = grid.GetMonstersNear(center, explosionRadius, target);

            foreach (Monster monster in nearby)
            {
                if (!monster.IsDead)
                    monster.TakeEffectDamage(damage);
            }
        }
    }
}
