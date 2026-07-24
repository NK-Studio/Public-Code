using BounceHeroes.Core;
using BounceHeroes.Data;
using BounceHeroes.Managers;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 볼 타격 한 건을 처리하는 데미지 파이프라인입니다.
    /// 기본 데미지 → 패시브 수정 → 치명타 판정 → 최종 데미지 적용 → 스킬 훅 순서로 진행됩니다.
    /// 정적 <c>GameManager</c>/<c>SkillManager</c> 참조 대신 필요한 의존을 생성자로 주입받습니다.
    /// </summary>
    public sealed class CombatService : ICombatService
    {
        private readonly GameBalanceData _balance;
        private readonly GridField _grid;
        private readonly ISkillService _skills;

        // BallLauncher는 주입받지 않는다. 타격을 만든 볼이 자신의 발사기를 이미 알고 있어(ball.Launcher)
        // 그걸 사용한다. 이렇게 하면 BallLauncher(→ICombatService) ↔ CombatService(→BallLauncher) 순환을 피한다.
        public CombatService(GameBalanceData balance, GridField grid, ISkillService skills)
        {
            _balance = balance;
            _grid = grid;
            _skills = skills;
        }

        public void ResolveHit(Ball ball, Monster monster, Vector2? contactNormal, Vector2 hitPoint)
        {
            if (monster.IsDead)
                return;

            DamageContext context = new DamageContext
            {
                BaseDamage = ball.Damage,
                PercentBonus = 0f,
                CritChance = _balance.BaseCritChance,
                CritMultiplier = _balance.CritDamageRate,
                IsNormalBall = ball.IsNormalBall,
                WallBounceStacks = ball.WallBounceStacks,
                Face = DetermineFace(contactNormal, hitPoint, monster.transform.position)
            };

            _skills.ApplyPassiveModifiers(ref context, monster);

            context.IsCrit = Random.value < context.CritChance;

            float damage = context.BaseDamage * (1f + context.PercentBonus);

            if (context.IsCrit)
                damage *= 1f + context.CritMultiplier;

            ball.ConsumeWallStacks();

            if (ball.Skill != null)
            {
                SkillHitContext hitContext = new SkillHitContext
                {
                    Target = monster,
                    HitPoint = hitPoint,
                    Level = ball.Level,
                    Grid = _grid,
                    Launcher = ball.Launcher
                };

                ball.Skill.OnMonsterHit(in hitContext);

                if (ball.Skill.HitSfxId != AudioId.None)
                    CombatEvents.SkillHitLanded?.Invoke(ball.Skill.HitSfxId, monster.transform.position);
            }

            monster.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)), context.IsCrit);
        }

        private static HitFace DetermineFace(Vector2? contactNormal, Vector2 hitPoint, Vector3 monsterCenter)
        {
            if (contactNormal.HasValue)
            {
                Vector2 normal = contactNormal.Value;

                if (normal.y < -0.35f)
                    return HitFace.Front;

                if (normal.y > 0.35f)
                    return HitFace.Back;

                return HitFace.Side;
            }

            float deltaY = hitPoint.y - monsterCenter.y;

            if (deltaY < -0.12f)
                return HitFace.Front;

            if (deltaY > 0.12f)
                return HitFace.Back;

            return HitFace.Side;
        }
    }
}
