using BounceHeroes.Core;
using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 볼 형태로 발사되는 액티브 스킬의 공통 데이터와 타격 훅을 정의하는 추상 설정 에셋입니다.
    /// </summary>
    public abstract class ActiveSkillData : SkillData
    {
        [Header("Active Ball")]
        [SerializeField] private Sprite ballSprite;
        [SerializeField] private BallVisualProfile ballVisualProfile;
        [SerializeField] private Ball ballPrefab;
        [SerializeField] private int[] ballDamage = new int[MaxLevel];

        [Header("Hit Sound")]
        [Tooltip("이 스킬 볼이 몬스터를 타격했을 때 재생할 사운드입니다. None이면 기본 타격음(MonsterHit/MonsterCrit)을 사용합니다.")]
        [SerializeField] private AudioId hitSfxId = AudioId.None;

        /// <summary>이 스킬 볼의 타격 사운드입니다. None이면 기본 타격음을 사용합니다.</summary>
        public AudioId HitSfxId => hitSfxId;

        /// <summary>이 스킬 볼의 스프라이트입니다.</summary>
        public Sprite BallSprite => ballVisualProfile != null && ballVisualProfile.Sprite != null
            ? ballVisualProfile.Sprite
            : ballSprite;

        public BallVisualProfile BallVisualProfile => ballVisualProfile;

        public Ball BallPrefab => ballPrefab;

        /// <summary>몬스터와 물리적으로 충돌(반사)하는지 여부입니다. 관통형 볼은 false입니다.</summary>
        public virtual bool ReflectsOffMonsters => true;

        /// <summary>
        /// 지정한 레벨의 볼 충돌 데미지를 반환합니다.
        /// </summary>
        /// <param name="level">1부터 시작하는 스킬 레벨</param>
        /// <returns>볼 충돌 데미지</returns>
        public int GetBallDamage(int level)
        {
            return ballDamage[Mathf.Clamp(level, 1, MaxLevel) - 1];
        }

        /// <summary>
        /// 이 볼이 몬스터를 타격했을 때의 추가 효과를 실행합니다.
        /// </summary>
        /// <param name="context">타격 상황 정보</param>
        public virtual void OnMonsterHit(in SkillHitContext context)
        {
        }
    }
}
