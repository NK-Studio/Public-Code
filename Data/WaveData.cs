using System;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 한 웨이브의 스폰 시퀀스와 3택지 노출 조건을 정의합니다.
    /// </summary>
    [Serializable]
    public class WaveData
    {
        /// <summary>이 웨이브에서 순서대로 스폰할 스텝 목록입니다.</summary>
        public SpawnStep[] Steps;

        /// <summary>이 처치 수에 도달하면 3택지 선택 UI가 노출됩니다.</summary>
        public int KillRequirement = 3;
    }
}
