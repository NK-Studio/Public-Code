using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using UnityEngine;

namespace BounceHeroes.FX
{
    /// <summary>
    /// 전역 이펙트 프리팹과 풀링 설정을 담는 데이터 자산입니다.
    /// VContainer에는 이 자산 하나만 등록하고, <see cref="FXService"/>가 이를 전달받아 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FXDatabase", menuName = "BounceHeroes/FX Database")]
    public sealed class FXDatabase : ScriptableObject
    {
        /// <summary>하나의 이펙트 항목: 식별자·프리팹·미리 생성 수·기본 수명.</summary>
        [Serializable]
        public struct Entry
        {
            public FXId id;
            public GameObject prefab;

            [Tooltip("시작 시 미리 생성해 둘 인스턴스 수")]
            [Min(0)] public int prewarm;

            [Tooltip("일회성 재생 시 이 시간(초) 뒤 자동으로 풀에 반환")]
            [Min(0f)] public float lifetime;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        /// <summary>등록된 이펙트 항목 목록입니다.</summary>
        public IReadOnlyList<Entry> Entries => entries;
    }
}
