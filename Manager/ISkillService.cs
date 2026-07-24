using System.Collections.Generic;
using BounceHeroes.Data;
using BounceHeroes.Gameplay;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 보유 스킬·패시브 훅·3택지 생성의 추상화입니다.
    /// 소비자는 <c>SkillManager.Instance</c> 정적 참조 대신 이 인터페이스를 주입받습니다.
    /// </summary>
    public interface ISkillService
    {
        /// <summary>발사 볼 구성에 포함할 보유 액티브 스킬과 레벨 목록을 반환합니다.</summary>
        List<(ActiveSkillData Skill, int Level)> GetActiveLoadout();

        /// <summary>3택지 전체 풀에 등장 가능한 액티브 스킬들의 볼 프리팹을 중복 없이 반환합니다.
        /// 아직 보유하지 않은 스킬의 타격 FX도 게임 시작 시 미리 생성해두기 위해 쓰입니다.</summary>
        IReadOnlyList<Ball> GetAllBallPrefabs();

        IReadOnlyList<SkillLoadoutEntry> GetActiveLoadoutEntries();

        IReadOnlyList<SkillLoadoutEntry> GetPassiveLoadoutEntries();

        /// <summary>보유한 모든 패시브 스킬로 데미지 컨텍스트를 수정합니다.</summary>
        void ApplyPassiveModifiers(ref DamageContext context, Monster target);

        /// <summary>몬스터 처치를 패시브 스킬에 전달합니다.</summary>
        void NotifyMonsterKilled(Monster monster);

        /// <summary>3택지 규칙에 따라 선택지를 생성합니다.</summary>
        SkillChoice[] BuildChoices();

        /// <summary>선택된 선택지를 적용합니다.</summary>
        void ApplyChoice(int index);

        /// <summary>3택지 규칙을 거치지 않고 특정 스킬을 지정 레벨로 즉시 보유시킵니다(테스트용).</summary>
        void DebugSetSkillLevel(SkillData skill, int level);
    }
}
