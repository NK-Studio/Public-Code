using BounceHeroes.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace BounceHeroes.FX
{
    /// <summary>부착형 이펙트 인스턴스를 풀로 되돌리는 핸들입니다.</summary>
    internal sealed class FXHandle : IFXHandle
    {
        private GameObject _instance;
        private IObjectPool<GameObject> _pool;

        public FXHandle(GameObject instance, IObjectPool<GameObject> pool)
        {
            _instance = instance;
            _pool = pool;
        }

        public void Release()
        {
            if (_instance == null || _pool == null)
                return;

            _pool.Release(_instance);
            _instance = null;
            _pool = null;
        }
    }

    /// <summary>데이터베이스에 없는 FXId를 요청했을 때 반환하는 무동작 핸들입니다.</summary>
    internal sealed class NullFXHandle : IFXHandle
    {
        public static readonly NullFXHandle Instance = new NullFXHandle();

        public void Release()
        {
        }
    }
}
