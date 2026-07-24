using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using FMODUnity;
using UnityEngine;

namespace BounceHeroes.Audio
{
    /// <summary>
    /// 사운드 id와 실제 FMOD 이벤트(또는 Key)를 매핑하는 데이터 자산입니다.
    /// VContainer에는 이 자산 하나만 등록하고, <see cref="AudioService"/>가 이를 전달받아 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AudioDatabase", menuName = "BounceHeroes/Audio Database")]
    public sealed class AudioDatabase : ScriptableObject
    {
        /// <summary>하나의 사운드 항목: 식별자·버스·재생 방식(EventReference 또는 Key).</summary>
        [Serializable]
        public struct Entry
        {
            public AudioId id;

            [Tooltip("논리 버스. Key 모드에서 조회할 KeyList와 재생 emitter를 결정합니다.")]
            public AudioBusType bus;

            [Tooltip("체크하면 Key로, 해제하면 아래 EventReference로 재생합니다.")]
            public bool useKey;

            [Tooltip("직접 지정 방식일 때 사용할 FMOD 이벤트")]
            public EventReference reference;

            [Tooltip("Key 방식일 때 조회할 키 문자열 (버스에 해당하는 KeyList에서 검색)")]
            public string key;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        /// <summary>등록된 사운드 항목 목록입니다.</summary>
        public IReadOnlyList<Entry> Entries => entries;
    }
}
