using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 스테이지 전체의 웨이브 구성을 보관하는 설정 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_WaveTable", menuName = "BounceHeroes/Wave Table")]
    public class WaveTableData : ScriptableObject
    {
        [SerializeField] private WaveData[] waves;

        private int _totalMonsterCount = -1;

        /// <summary>전체 웨이브 수입니다.</summary>
        public int WaveCount => waves?.Length ?? 0;

        /// <summary>
        /// 전체 웨이브에 걸쳐 등록된 몬스터의 총 마리 수입니다. 스테이지 도착(진행률 100%) 판정 기준으로 쓰입니다.
        /// </summary>
        public int TotalMonsterCount
        {
            get
            {
                if (_totalMonsterCount < 0)
                    _totalMonsterCount = CountTotalMonsters();

                return _totalMonsterCount;
            }
        }

        /// <summary>
        /// 지정한 인덱스의 웨이브 데이터를 반환합니다.
        /// </summary>
        /// <param name="index">0부터 시작하는 웨이브 인덱스</param>
        /// <returns>해당 웨이브 데이터</returns>
        public WaveData GetWave(int index)
        {
            return waves[index];
        }

        private int CountTotalMonsters()
        {
            if (waves == null)
                return 0;

            int count = 0;

            foreach (WaveData wave in waves)
            {
                if (wave?.Steps == null)
                    continue;

                foreach (SpawnStep step in wave.Steps)
                {
                    if (step?.Placements == null)
                        continue;

                    foreach (PlacedMonster placement in step.Placements)
                    {
                        if (placement?.Monster != null)
                            count++;
                    }
                }
            }

            return count;
        }
    }
}
