using System;
using UnityEngine;

namespace BounceHeroes.Core
{
    /// <summary>
    /// 전투 중 발생하는 연출용 신호를 전달하는 이벤트 허브입니다.
    /// 저즈(히트스톱, 셰이크, VFX) 계층이 구독합니다.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>볼 타격이 적중했을 때 호출됩니다. (위치, 치명타 여부)</summary>
        public static Action<Vector3, bool> MonsterHitLanded;

        /// <summary>몬스터가 처치되었을 때 호출됩니다. (위치)</summary>
        public static Action<Vector3> MonsterKilled;

        /// <summary>레이저 볼이 행 전체를 타격했을 때 호출됩니다. (행의 월드 Y)</summary>
        public static Action<float> RowLaserFired;

        /// <summary>마지막 성냥 폭발이 발생했을 때 호출됩니다. (위치, 반경)</summary>
        public static Action<Vector3, float> ExplosionFired;

        /// <summary>웨이브의 마지막 몬스터가 처치되었을 때 호출됩니다. (위치)</summary>
        public static Action<Vector3> WaveLastKill;

        /// <summary>볼이 발사되었을 때 호출됩니다. (발사 위치, 발사 방향)</summary>
        public static Action<Vector3, Vector2> BallFired;

        /// <summary>몬스터가 하단에 도달해 플레이어가 피해를 입었을 때 호출됩니다. (플레이어 위치)</summary>
        public static Action<Vector3> PlayerHit;

        /// <summary>
        /// 액티브 스킬 볼이 몬스터를 타격해 스킬 전용 사운드를 재생해야 할 때 호출됩니다. (사운드 id, 위치)
        /// 스킬의 HitSfxId가 <see cref="AudioId.None"/>이 아닐 때만 발행됩니다.
        /// </summary>
        public static Action<AudioId, Vector3> SkillHitLanded;
    }
}
