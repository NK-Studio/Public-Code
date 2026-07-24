namespace BounceHeroes.Data
{
    /// <summary>
    /// 한 번의 타격에 대한 데미지 계산 정보를 담는 컨텍스트입니다.
    /// 패시브 스킬들이 이 컨텍스트를 순서대로 수정한 뒤 최종 데미지가 확정됩니다.
    /// </summary>
    public struct DamageContext
    {
        /// <summary>볼 자체의 기본 데미지입니다.</summary>
        public int BaseDamage;

        /// <summary>합연산으로 누적되는 퍼센트 보너스입니다. (0.2 = +20%)</summary>
        public float PercentBonus;

        /// <summary>치명타 확률입니다. (0~1)</summary>
        public float CritChance;

        /// <summary>치명타 시 추가 데미지 비율입니다. (0.5 = +50%)</summary>
        public float CritMultiplier;

        /// <summary>노멀 볼 여부입니다. 패시브(따뜻한 양철 심장) 판정에 사용됩니다.</summary>
        public bool IsNormalBall;

        /// <summary>타격 전까지 볼이 벽에 튕긴 횟수입니다. 패시브(마법 거울) 판정에 사용됩니다.</summary>
        public int WallBounceStacks;

        /// <summary>몬스터의 어느 면을 타격했는지입니다.</summary>
        public HitFace Face;

        /// <summary>치명타 판정 결과입니다. 파이프라인 마지막에 확정됩니다.</summary>
        public bool IsCrit;
    }
}
