using System.Collections.Generic;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 몬스터가 내려오는 필드입니다.
    /// 가로는 열(레인) 단위로 정렬되지만, 세로는 연속(float)으로 내려옵니다.
    /// 레이저(같은 행)·폭발 같은 범위 판정은 논리 좌표가 아니라 실제 월드 Y 밴드로 계산합니다.
    /// </summary>
    public sealed class GridField : MonoBehaviour
    {
        [SerializeField] private int columns = 8;
        [SerializeField] private int rows = 13;
        [SerializeField] private float cellWidth = 0.9844f;
        [SerializeField] private float cellHeight = 0.9747f;

        // 인접한 열(레인)의 몬스터는 가로 폭 절반의 합이 칸 하나 간격과 거의 같아, 부동소수점 오차로
        // 옆 칸 몬스터까지 같은 레인으로 오판해 정지하는 문제가 있었다. 여유를 두어 실제로 겹치는
        // 같은 레인끼리만 차단 대상으로 잡히게 한다.
        private const float LaneOverlapMargin = 0.9f;

        private readonly List<Monster> _monsters = new List<Monster>();

        /// <summary>가로 열(레인) 수입니다.</summary>
        public int Columns => columns;

        /// <summary>필드의 세로 칸 수입니다. 상단~위험선 사이 높이를 정의합니다.</summary>
        public int Rows => rows;

        /// <summary>한 열의 가로 폭(월드 단위)입니다.</summary>
        public float CellWidth => cellWidth;

        /// <summary>한 칸의 세로 높이(월드 단위)입니다. 레이저 행 밴드 두께의 기준입니다.</summary>
        public float CellHeight => cellHeight;

        /// <summary>현재 살아있는 몬스터 수입니다.</summary>
        public int AliveCount => _monsters.Count;

        /// <summary>몬스터가 스폰되어 내려오기 시작하는 최상단 월드 Y입니다.</summary>
        public float TopY => transform.position.y + cellHeight;

        /// <summary>몬스터가 이 Y 아래로 내려가면 플레이어가 피해를 입는 위험선입니다.</summary>
        public float DangerY => transform.position.y - (rows - 1) * cellHeight;

        /// <summary>
        /// 지정한 열의 월드 X 좌표를 반환합니다.
        /// </summary>
        /// <param name="col">0부터 시작하는 열</param>
        /// <returns>열 중심의 월드 X</returns>
        public float ColumnToX(int col)
        {
            return transform.position.x + (col - (columns - 1) * 0.5f) * cellWidth;
        }

        /// <summary>
        /// 논리 (행, 열)을 월드 좌표로 변환합니다. 스폰 포메이션 배치에 사용합니다.
        /// </summary>
        /// <param name="row">0부터 시작하는 행(위에서 아래로 증가)</param>
        /// <param name="col">0부터 시작하는 열</param>
        /// <returns>칸 중심의 월드 좌표</returns>
        public Vector3 CellToWorld(int row, int col)
        {
            return new Vector3(ColumnToX(col), transform.position.y - row * cellHeight, 0f);
        }

        /// <summary>
        /// Footprint 전체가 차지하는 셀 영역의 중심 월드 좌표를 반환합니다.
        /// </summary>
        /// <param name="row">Footprint의 최상단 행입니다.</param>
        /// <param name="col">Footprint의 가장 왼쪽 열입니다.</param>
        /// <param name="width">Footprint의 가로 셀 수입니다.</param>
        /// <param name="height">Footprint의 세로 셀 수입니다.</param>
        /// <returns>Footprint 중심의 월드 좌표입니다.</returns>
        public Vector3 FootprintToWorldCenter(int row, int col, int width, int height)
        {
            int safeWidth = Mathf.Max(1, width);
            int safeHeight = Mathf.Max(1, height);
            Vector3 origin = CellToWorld(row, col);

            return origin + new Vector3(
                (safeWidth - 1) * cellWidth * 0.5f,
                -(safeHeight - 1) * cellHeight * 0.5f,
                0f);
        }

        /// <summary>
        /// 몬스터를 필드에 등록합니다. 범위 판정 대상 목록에 포함됩니다.
        /// </summary>
        /// <param name="monster">등록할 몬스터</param>
        public void Register(Monster monster)
        {
            _monsters.Add(monster);
        }

        /// <summary>
        /// 몬스터를 필드에서 제거합니다.
        /// </summary>
        /// <param name="monster">제거할 몬스터</param>
        public void Unregister(Monster monster)
        {
            _monsters.Remove(monster);
        }

        /// <summary>
        /// 지정한 월드 Y와 같은 '행'(밴드) 안에 있는 살아있는 몬스터 목록을 반환합니다.
        /// </summary>
        /// <param name="worldY">기준 월드 Y</param>
        /// <param name="halfBand">밴드 절반 두께. 보통 CellHeight * 0.5</param>
        /// <returns>밴드 안의 몬스터 목록</returns>
        public List<Monster> GetMonstersInBand(float worldY, float halfBand)
        {
            List<Monster> result = new List<Monster>();

            foreach (Monster monster in _monsters)
            {
                if (!monster.IsDead && Mathf.Abs(monster.transform.position.y - worldY) <= halfBand)
                    result.Add(monster);
            }

            return result;
        }

        /// <summary>
        /// 같은 열(가로 폭이 겹치는)에서 자신보다 아래에 있는 가장 가까운 살아있는 몬스터를 반환합니다.
        /// 몬스터가 아래쪽 개체와 겹치지 않도록 정지시키는 데 사용합니다.
        /// </summary>
        /// <param name="self">기준이 되는 몬스터(결과에서 제외)</param>
        /// <param name="worldX">기준 몬스터의 월드 X</param>
        /// <param name="halfWidth">기준 몬스터의 가로 폭 절반(월드 단위)</param>
        /// <returns>가로로 겹치면서 바로 아래에 있는 몬스터. 없으면 null</returns>
        public Monster GetBlockerBelow(Monster self, float worldX, float halfWidth)
        {
            Monster nearest = null;
            float bestY = float.NegativeInfinity;

            foreach (Monster monster in _monsters)
            {
                if (monster == self || monster.IsDead)
                    continue;

                if (monster.transform.position.y >= self.transform.position.y)
                    continue;

                float xOverlap = (halfWidth + monster.HalfWidth) * LaneOverlapMargin;

                if (Mathf.Abs(monster.transform.position.x - worldX) >= xOverlap)
                    continue;

                if (monster.transform.position.y > bestY)
                {
                    bestY = monster.transform.position.y;
                    nearest = monster;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 지정한 위치 주변의 살아있는 몬스터 목록을 반환합니다.
        /// </summary>
        /// <param name="center">중심 월드 좌표</param>
        /// <param name="radius">탐색 반경</param>
        /// <param name="exclude">제외할 몬스터</param>
        /// <returns>반경 안의 몬스터 목록</returns>
        public List<Monster> GetMonstersNear(Vector3 center, float radius, Monster exclude)
        {
            List<Monster> result = new List<Monster>();
            float sqrRadius = radius * radius;

            foreach (Monster monster in _monsters)
            {
                if (monster == exclude || monster.IsDead)
                    continue;

                if ((monster.transform.position - center).sqrMagnitude <= sqrRadius)
                    result.Add(monster);
            }

            return result;
        }
    }
}
