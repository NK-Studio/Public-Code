using BounceHeroes.Gameplay;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 클러스터 볼 스킬입니다. 적 타격 시 확률적으로 소형 특수 볼을 생성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Skill_ClusterBall", menuName = "BounceHeroes/Skills/Cluster Ball")]
    public class ClusterBallSkillData : ActiveSkillData
    {
        [Header("Cluster")]
        [SerializeField] private float[] spawnChance = { 0.4f, 0.5f, 0.6f };
        [SerializeField] private int[] miniBallDamage = { 10, 15, 20 };

        [Header("Mini Ball")]
        [SerializeField] private Ball miniBallPrefab;
        [SerializeField] private Sprite[] miniBallSprites;
        [SerializeField] private float miniBallSpeed = 12f;
        [SerializeField] private float miniBallFlightTime = 3f;

        /// <summary>
        /// 확률 판정 후 타격 지점에 소형 특수 볼을 생성합니다.
        /// 소형 볼의 프리팹·스프라이트·속도는 이 스킬 데이터가 소유하며, 발사기에 스폰만 위임합니다.
        /// </summary>
        /// <param name="context">타격 상황 정보</param>
        public override void OnMonsterHit(in SkillHitContext context)
        {
            int index = context.Level - 1;

            if (Random.value > spawnChance[index])
                return;

            context.Launcher.SpawnMiniBall(
                miniBallPrefab, miniBallSprites, context.HitPoint,
                miniBallDamage[index], miniBallSpeed, miniBallFlightTime);
        }
    }
}
