using System.Collections.Generic;
using BounceHeroes.Data;
using UnityEngine;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 3택지 선택지 생성 규칙을 담당하는 순수 로직 서비스입니다.
    /// 규칙: 미보유 스킬은 Lv.1만, 최대 보유 수 도달 시 보유 스킬의 업그레이드만,
    /// 동일 스킬 카드는 중복 노출되지 않습니다.
    /// </summary>
    public static class SkillSelectionService
    {
        private const int ChoiceCount = 3;

        /// <summary>
        /// 현재 보유 상태를 기준으로 선택지 후보를 만들고 무작위로 최대 3개를 추출합니다.
        /// </summary>
        /// <param name="pool">전체 스킬 풀</param>
        /// <param name="levels">보유 스킬과 레벨</param>
        /// <param name="activeCount">보유 중인 액티브 스킬 수</param>
        /// <param name="passiveCount">보유 중인 패시브 스킬 수</param>
        /// <param name="maxActive">액티브 스킬 최대 보유 수</param>
        /// <param name="maxPassive">패시브 스킬 최대 보유 수</param>
        /// <returns>최대 3개의 선택지 배열</returns>
        public static SkillChoice[] BuildChoices(
            IReadOnlyList<SkillData> pool,
            IReadOnlyDictionary<SkillData, int> levels,
            int activeCount,
            int passiveCount,
            int maxActive,
            int maxPassive)
        {
            HashSet<SkillData> owned = new HashSet<SkillData>(levels.Keys);
            List<SkillChoice> candidates = new List<SkillChoice>();

            foreach (SkillData skill in pool)
            {
                if (skill == null)
                    continue;

                if (levels.TryGetValue(skill, out int level))
                {
                    if (level >= SkillData.MaxLevel)
                        continue;

                    candidates.Add(new SkillChoice
                    {
                        Skill = skill,
                        TargetLevel = level + 1,
                        IsUpgrade = true,
                        SynergyText = skill.GetSynergyText(owned)
                    });

                    continue;
                }

                bool isActive = skill is ActiveSkillData;

                if (isActive && activeCount >= maxActive)
                    continue;

                if (!isActive && passiveCount >= maxPassive)
                    continue;

                candidates.Add(new SkillChoice
                {
                    Skill = skill,
                    TargetLevel = 1,
                    IsUpgrade = false,
                    SynergyText = skill.GetSynergyText(owned)
                });
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int count = Mathf.Min(ChoiceCount, candidates.Count);
            SkillChoice[] result = new SkillChoice[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = candidates[i];
            }

            return result;
        }
    }
}
