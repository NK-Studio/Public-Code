namespace BounceHeroes.Data
{
    /// <summary>
    /// 3택지 선택 UI에 노출되는 하나의 선택지 정보입니다.
    /// </summary>
    public struct SkillChoice
    {
        /// <summary>선택지에 해당하는 스킬 데이터입니다.</summary>
        public SkillData Skill;

        /// <summary>선택 시 도달하는 레벨입니다. (신규 획득이면 1)</summary>
        public int TargetLevel;

        /// <summary>이미 보유한 스킬의 업그레이드 선택지인지 여부입니다.</summary>
        public bool IsUpgrade;

        /// <summary>보유 스킬과의 시너지 안내 문구입니다. 시너지가 없으면 null입니다.</summary>
        public string SynergyText;
    }

    public readonly struct SkillLoadoutEntry
    {
        public SkillLoadoutEntry(SkillData skill, int level)
        {
            Skill = skill;
            Level = level;
        }

        public SkillData Skill { get; }
        public int Level { get; }
    }
}
