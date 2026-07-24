using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 볼이 반사되는 벽입니다. 고스트 볼의 수동 반사 계산에 사용할 법선을 보관합니다.
    /// </summary>
    public sealed class Wall : MonoBehaviour
    {
        [SerializeField] private Vector2 normal = Vector2.down;

        /// <summary>벽의 안쪽(필드 방향) 법선 벡터입니다.</summary>
        public Vector2 Normal => normal.normalized;
    }
}
