using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace BounceHeroes.FX
{
    /// <summary>
    /// 프리팹별 오브젝트 풀로 이펙트를 재생하는 서비스입니다.
    /// 풀의 책임(미리 생성 / 꺼내기 / 반환 / 부족하면 추가 생성)만 담당하며,
    /// "무슨 프리팹·수명" 같은 결정은 <see cref="FXDatabase"/>와 호출자가 가집니다.
    /// </summary>
    public sealed class FXService : IFXService, IDisposable
    {
        private sealed class Pool
        {
            public IObjectPool<GameObject> Objects;
            public Vector3 BaseScale;
        }

        private const int DefaultCapacity = 8;
        private const int MaxSize = 128;

        private readonly Dictionary<GameObject, Pool> _pools = new();
        private readonly Dictionary<FXId, FXDatabase.Entry> _byId = new();
        private readonly Transform _root;

        public FXService(FXDatabase database)
        {
            _root = new GameObject("[FX Pool]").transform;

            if (database == null)
                return;

            foreach (FXDatabase.Entry entry in database.Entries)
            {
                if (entry.prefab == null || entry.id == FXId.None)
                    continue;

                _byId[entry.id] = entry;
                Pool pool = GetOrCreatePool(entry.prefab);
                Prewarm(pool, entry.prewarm);
            }
        }

        public void Play(FXId id, Vector3 position)
            => Play(id, position, Quaternion.identity, 1f);

        public void Play(FXId id, Vector3 position, Quaternion rotation, float scale)
        {
            if (!_byId.TryGetValue(id, out FXDatabase.Entry entry) || entry.prefab == null)
                return;

            Play(entry.prefab, position, rotation, scale, entry.lifetime);
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float lifetime)
        {
            if (prefab == null)
                return;

            Pool pool = GetOrCreatePool(prefab);
            GameObject go = pool.Objects.Get();

            Transform t = go.transform;
            t.SetParent(_root, false);
            t.SetPositionAndRotation(position, rotation);
            t.localScale = pool.BaseScale * scale;
            go.SetActive(true);

            go.GetComponent<PooledFX>().PlayOneShot(lifetime, () => pool.Objects.Release(go));
        }

        public IFXHandle PlayAttached(FXId id, Transform parent)
        {
            if (!_byId.TryGetValue(id, out FXDatabase.Entry entry) || entry.prefab == null)
                return NullFXHandle.Instance;

            Pool pool = GetOrCreatePool(entry.prefab);
            GameObject go = pool.Objects.Get();

            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = pool.BaseScale;
            go.SetActive(true);

            go.GetComponent<PooledFX>().PlayPersistent();
            return new FXHandle(go, pool.Objects);
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            _pools.Clear();
            _byId.Clear();
        }

        private Pool GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool existing))
                return existing;

            Pool pool = new Pool { BaseScale = prefab.transform.localScale };
            pool.Objects = new ObjectPool<GameObject>(
                createFunc: () => CreateInstance(prefab),
                actionOnGet: null,
                actionOnRelease: ReturnInstance,
                actionOnDestroy: DestroyInstance,
                collectionCheck: false,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxSize);

            _pools[prefab] = pool;
            return pool;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab, _root);

            if (go.GetComponent<PooledFX>() == null)
                go.AddComponent<PooledFX>();

            go.SetActive(false);
            return go;
        }

        private void ReturnInstance(GameObject go)
        {
            if (go == null)
                return;

            PooledFX pooled = go.GetComponent<PooledFX>();

            if (pooled != null)
                pooled.OnReturned();

            go.transform.SetParent(_root, false);
            go.SetActive(false);
        }

        private static void DestroyInstance(GameObject go)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }

        private static void Prewarm(Pool pool, int count)
        {
            if (count <= 0)
                return;

            List<GameObject> temp = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
                temp.Add(pool.Objects.Get());

            foreach (GameObject go in temp)
                pool.Objects.Release(go);
        }
    }
}
