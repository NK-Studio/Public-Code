using BounceHeroes.Gameplay;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 데미지 파이프라인과 처치 이벤트에 개입하는 패시브 스킬의 공통 훅을 정의하는 추상 설정 에셋입니다.
    /// </summary>
    public abstract class PassiveSkillData : SkillData
    {
        /// <summary>
        /// 데미지 계산 파이프라인에서 컨텍스트를 수정합니다.
        /// </summary>
        /// <param name="context">수정할 데미지 컨텍스트</param>
        /// <param name="target">타격 대상 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        public virtual void ModifyDamage(ref DamageContext context, Monster target, int level)
        {
        }

        /// <summary>
        /// 몬스터가 처치되었을 때 호출됩니다.
        /// </summary>
        /// <param name="target">처치된 몬스터</param>
        /// <param name="level">현재 스킬 레벨</param>
        /// <param name="grid">격자 필드 참조</param>
        public virtual void OnMonsterKilled(Monster target, int level, GridField grid)
        {
        }
    }
}
