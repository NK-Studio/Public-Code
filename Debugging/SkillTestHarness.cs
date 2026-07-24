using System;
using BounceHeroes.Data;
using BounceHeroes.Managers;
using UnityEngine;

namespace BounceHeroes.Debugging
{
    /// <summary>
    /// 인스펙터에서 지정한 스킬을 원하는 레벨로 즉시 보유시키는 테스트 도구입니다.
    /// 3택지 뽑기 없이 패시브/액티브 스킬의 레벨별 효과를 바로 확인할 때 사용합니다.
    /// skillPool에 등록되지 않은 새 스킬 에셋도 그대로 드래그해 테스트할 수 있습니다.
    /// </summary>
    public sealed class SkillTestHarness : MonoBehaviour
    {
        [Serializable]
        private struct SkillLevelOverride
        {
            public SkillData skill;
            [Range(1, SkillData.MaxLevel)] public int level;
        }

        [SerializeField] private SkillLevelOverride[] overrides;
        [Tooltip("플레이 중 인스펙터에서 레벨을 바꾸면 즉시 재적용합니다.")]
        [SerializeField] private bool applyOnValidate = true;

        private void OnValidate()
        {
            if (!applyOnValidate || !Application.isPlaying)
                return;

            Apply();
        }

        [ContextMenu("Apply Skill Overrides")]
        public void Apply()
        {
            // 디버그 도구는 OnValidate/컨텍스트 메뉴 등 DI 바깥에서 호출되므로 씬에서 직접 찾는다.
            ISkillService skills = FindFirstObjectByType<SkillManager>();

            if (skills == null)
            {
                Debug.LogWarning("SkillTestHarness: 씬에서 SkillManager를 찾지 못했습니다.", this);
                return;
            }

            foreach (SkillLevelOverride entry in overrides)
            {
                if (entry.skill == null)
                    continue;

                skills.DebugSetSkillLevel(entry.skill, entry.level);
                Debug.Log($"SkillTestHarness: {entry.skill.DisplayName} -> Lv.{entry.level}", this);
            }
        }
    }
}
