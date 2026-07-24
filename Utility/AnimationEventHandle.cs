using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    [System.Serializable]
    public sealed class StringByEvent
    {
        public string Key;
        public UnityEvent UnityEvent;
    }

    public sealed class AnimationEventHandle : MonoBehaviour
    {
        [SerializeField] private StringByEvent[] stringByEvents;

        private readonly Dictionary<string, UnityEvent> eventsByKey = new();

        private void Awake()
        {
            eventsByKey.Clear();

            if (stringByEvents == null)
            {
                return;
            }

            foreach (StringByEvent stringByEvent in stringByEvents)
            {
                if (stringByEvent == null || string.IsNullOrWhiteSpace(stringByEvent.Key))
                {
                    continue;
                }

                if (eventsByKey.ContainsKey(stringByEvent.Key))
                {
                    Debug.LogWarning($"Duplicate key {stringByEvent.Key} at {gameObject.name} !!!", this);
                    continue;
                }

                eventsByKey.Add(stringByEvent.Key, stringByEvent.UnityEvent);
            }
        }

        public void OnAnimationEvent(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (eventsByKey.TryGetValue(id, out UnityEvent unityEvent))
            {
                unityEvent?.Invoke();
            }
        }
    }
}
