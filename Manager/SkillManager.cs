using System.Collections.Generic;
using BounceHeroes.Data;
using BounceHeroes.Gameplay;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 보유 스킬과 레벨을 관리하고, 패시브 훅과 3택지 생성을 중계합니다.
    /// </summary>
    public sealed class SkillManager : MonoBehaviour, ISkillService
    {
        [SerializeField] private SkillData[] skillPool;
        [SerializeField] private int maxActiveSkills = 5;
        [SerializeField] private int maxPassiveSkills = 2;

        private readonly Dictionary<SkillData, int> _levels = new Dictionary<SkillData, int>();
        private readonly List<ActiveSkillData> _ownedActives = new List<ActiveSkillData>();
        private readonly List<PassiveSkillData> _ownedPassives = new List<PassiveSkillData>();
        private SkillChoice[] _pendingChoices;
        private GridField _grid;

        [Inject]
        public void Construct(GridField grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// 발사 볼 구성에 포함할 보유 액티브 스킬과 레벨 목록을 반환합니다.
        /// </summary>
        /// <returns>(스킬, 레벨) 목록. 획득 순서를 유지합니다.</returns>
        public List<(ActiveSkillData Skill, int Level)> GetActiveLoadout()
        {
            List<(ActiveSkillData, int)> loadout = new List<(ActiveSkillData, int)>();

            foreach (ActiveSkillData skill in _ownedActives)
            {
                loadout.Add((skill, _levels[skill]));
            }

            return loadout;
        }

        public IReadOnlyList<Ball> GetAllBallPrefabs()
        {
            List<Ball> prefabs = new List<Ball>();

            foreach (SkillData skill in skillPool)
            {
                if (skill is ActiveSkillData active && active.BallPrefab != null && !prefabs.Contains(active.BallPrefab))
                    prefabs.Add(active.BallPrefab);
            }

            return prefabs;
        }

        public IReadOnlyList<SkillLoadoutEntry> GetActiveLoadoutEntries()
        {
            return BuildLoadoutEntries(_ownedActives);
        }

        public IReadOnlyList<SkillLoadoutEntry> GetPassiveLoadoutEntries()
        {
            return BuildLoadoutEntries(_ownedPassives);
        }

        /// <summary>
        /// 보유한 모든 패시브 스킬로 데미지 컨텍스트를 수정합니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        public void ApplyPassiveModifiers(ref DamageContext context, Monster target)
        {
            foreach (PassiveSkillData passive in _ownedPassives)
            {
                passive.ModifyDamage(ref context, target, _levels[passive]);
            }
        }

        /// <summary>
        /// 몬스터 처치를 패시브 스킬(마지막 성냥 등)에 전달합니다.
        /// </summary>
        /// <param name="monster">처치된 몬스터</param>
        public void NotifyMonsterKilled(Monster monster)
        {
            foreach (PassiveSkillData passive in _ownedPassives)
            {
                passive.OnMonsterKilled(monster, _levels[passive], _grid);
            }
        }

        /// <summary>
        /// 3택지 규칙에 따라 선택지를 생성합니다.
        /// </summary>
        /// <returns>최대 3개의 선택지. 뽑을 수 있는 스킬이 없으면 빈 배열</returns>
        public SkillChoice[] BuildChoices()
        {
            _pendingChoices = SkillSelectionService.BuildChoices(
                skillPool, _levels, _ownedActives.Count, _ownedPassives.Count,
                maxActiveSkills, maxPassiveSkills);

            return _pendingChoices;
        }

        /// <summary>
        /// 선택된 선택지를 적용합니다. 신규 획득 또는 레벨업이 반영됩니다.
        /// </summary>
        /// <param name="index">선택한 선택지 인덱스</param>
        public void ApplyChoice(int index)
        {
            if (_pendingChoices == null || index < 0 || index >= _pendingChoices.Length)
                return;

            SkillChoice choice = _pendingChoices[index];
            _pendingChoices = null;

            SetSkillLevel(choice.Skill, choice.TargetLevel);
        }

        /// <summary>
        /// 3택지 규칙(보유 수 제한, 랜덤 추출 등)을 거치지 않고 특정 스킬을 원하는 레벨로 즉시 보유시킵니다.
        /// 테스트 씬에서 패시브/액티브 스킬을 레벨별로 바로 확인할 때 사용합니다.
        /// </summary>
        /// <param name="skill">보유시킬 스킬</param>
        /// <param name="level">설정할 레벨 (1~SkillData.MaxLevel로 clamp됩니다)</param>
        public void DebugSetSkillLevel(SkillData skill, int level)
        {
            if (skill == null)
                return;

            SetSkillLevel(skill, Mathf.Clamp(level, 1, SkillData.MaxLevel));
        }

        private void SetSkillLevel(SkillData skill, int level)
        {
            bool isNewlyOwned = !_levels.ContainsKey(skill);
            _levels[skill] = level;

            if (isNewlyOwned)
            {
                if (skill is ActiveSkillData active)
                    _ownedActives.Add(active);
                else if (skill is PassiveSkillData passive)
                    _ownedPassives.Add(passive);
            }

            GameplayEvents.SkillAcquired?.Invoke(skill, level);
            GameplayEvents.SkillLoadoutChanged?.Invoke(GetActiveLoadoutEntries(), GetPassiveLoadoutEntries());
        }

        private IReadOnlyList<SkillLoadoutEntry> BuildLoadoutEntries<TSkill>(IEnumerable<TSkill> skills)
            where TSkill : SkillData
        {
            List<SkillLoadoutEntry> entries = new List<SkillLoadoutEntry>();

            foreach (TSkill skill in skills)
            {
                entries.Add(new SkillLoadoutEntry(skill, _levels[skill]));
            }

            return entries;
        }
    }
}
