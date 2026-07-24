using System;
using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 웨이브 디자이너 격자 위에 배치한 몬스터 한 마리의 스폰 정보입니다.
    /// </summary>
    [Serializable]
    public class PlacedMonster
    {
        /// <summary>스폰할 몬스터 데이터입니다.</summary>
        public MonsterData Monster;

        /// <summary>저작 격자에서 0부터 시작하는 행(위에서 아래로 증가)입니다.</summary>
        public int Row;

        /// <summary>저작 격자에서 0부터 시작하는 열입니다.</summary>
        public int Col;

        /// <summary>0보다 크면 기본 체력 대신 이 값을 사용합니다.</summary>
        public int HpOverride;

        /// <summary>시계 방향 배치 회전(0/90/180/270도)입니다.</summary>
        public int Rotation;

        /// <summary>
        /// 회전을 반영한 실제 점유 가로/세로 칸 수를 계산합니다. 90도/270도면 가로세로가 뒤바뀝니다.
        /// </summary>
        /// <param name="monster">기준이 되는 몬스터 데이터</param>
        /// <param name="rotationDegrees">시계 방향 회전각(0/90/180/270)</param>
        /// <param name="width">계산된 실제 가로 칸 수</param>
        /// <param name="height">계산된 실제 세로 칸 수</param>
        public static void GetRotatedFootprint(MonsterData monster, int rotationDegrees, out int width, out int height)
        {
            GetRotatedFootprint(monster.FootprintWidth, monster.FootprintHeight, rotationDegrees, out width, out height);
        }

        /// <summary>
        /// 원본 가로/세로 칸 수를 회전에 맞춰 뒤바꿉니다. 90도/270도면 가로세로가 뒤바뀝니다.
        /// </summary>
        /// <param name="baseWidth">회전 전 가로 칸 수</param>
        /// <param name="baseHeight">회전 전 세로 칸 수</param>
        /// <param name="rotationDegrees">시계 방향 회전각(0/90/180/270)</param>
        /// <param name="width">계산된 실제 가로 칸 수</param>
        /// <param name="height">계산된 실제 세로 칸 수</param>
        public static void GetRotatedFootprint(int baseWidth, int baseHeight, int rotationDegrees, out int width, out int height)
        {
            baseWidth = Mathf.Max(1, baseWidth);
            baseHeight = Mathf.Max(1, baseHeight);
            bool swapped = rotationDegrees == 90 || rotationDegrees == 270;

            width = swapped ? baseHeight : baseWidth;
            height = swapped ? baseWidth : baseHeight;
        }
    }
}
