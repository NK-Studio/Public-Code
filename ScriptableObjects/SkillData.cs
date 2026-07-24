using System.Collections.Generic;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 모든 스킬의 공통 정보(이름, 아이콘, 레벨별 설명, 시너지 힌트)를 정의하는 추상 설정 에셋입니다.
    /// </summary>
    public abstract class SkillData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string[] levelDescriptions = new string[MaxLevel];

        [Header("Synergy Hint")]
        [SerializeField] private SkillData[] synergyWith;
        [SerializeField] private string synergyText;

        /// <summary>스킬의 최대 레벨입니다.</summary>
        public const int MaxLevel = 3;

        /// <summary>스킬의 표시 이름입니다.</summary>
        public string DisplayName => displayName;

        /// <summary>스킬 아이콘입니다.</summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 지정한 레벨의 효과 설명을 반환합니다.
        /// </summary>
        /// <param name="level">1부터 시작하는 스킬 레벨</param>
        /// <returns>해당 레벨의 효과 설명</returns>
        public string GetDescription(int level)
        {
            int index = Mathf.Clamp(level, 1, levelDescriptions.Length) - 1;
            return levelDescriptions[index];
        }

        /// <summary>
        /// 보유 스킬 목록에 시너지 대상 스킬이 있으면 시너지 문구를 반환합니다.
        /// </summary>
        /// <param name="ownedSkills">현재 보유 중인 스킬 목록</param>
        /// <returns>시너지 안내 문구. 시너지가 없으면 null</returns>
        public string GetSynergyText(ICollection<SkillData> ownedSkills)
        {
            if (synergyWith == null || string.IsNullOrEmpty(synergyText))
                return null;

            foreach (SkillData skill in synergyWith)
            {
                if (skill != null && ownedSkills.Contains(skill))
                    return synergyText;
            }

            return null;
        }
    }
}
