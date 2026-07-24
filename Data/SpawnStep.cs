using System;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 동시에 스폰되는 몬스터 묶음 하나를 정의합니다. 한 마리짜리 스텝도,
    /// 다구리로 한 번에 쏟아지는 스텝도 모두 이 하나의 구조로 표현합니다.
    /// 이전 스텝에서 스폰된 몬스터 전원이 기준 행을 통과하거나 제거되어야
    /// 다음 스텝이 스폰됩니다.
    /// </summary>
    [Serializable]
    public class SpawnStep
    {
        /// <summary>이 스텝에서 동시에 스폰할 몬스터 배치 목록입니다.</summary>
        public PlacedMonster[] Placements;
    }
}
