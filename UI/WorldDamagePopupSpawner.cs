using System.Collections.Generic;
using BounceHeroes.Data;
using UnityEngine;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 데미지 팝업 요청을 받아 월드 스페이스 스프라이트 숫자 팝업을 풀링해 재생합니다.
    /// </summary>
    public sealed class WorldDamagePopupSpawner : MonoBehaviour
    {
        private const float JitterX = 0.45f;
        private const float JitterYMin = 0.1f;
        private const float JitterYMax = 0.5f;

        [SerializeField] private WorldDamagePopup popupPrefab;
        [SerializeField] private Transform poolRoot;

        private readonly Stack<WorldDamagePopup> _pool = new Stack<WorldDamagePopup>();

        private void OnEnable()
        {
            GameplayEvents.DamagePopupRequested += OnDamagePopupRequested;
        }

        private void OnDisable()
        {
            GameplayEvents.DamagePopupRequested -= OnDamagePopupRequested;
        }

        private void OnDamagePopupRequested(Vector3 worldPosition, int amount, DamagePopupType type)
        {
            WorldDamagePopup popup = _pool.Count > 0 ? _pool.Pop() : CreatePopup();

            Vector3 jittered = worldPosition + new Vector3(
                Random.Range(-JitterX, JitterX),
                Random.Range(JitterYMin, JitterYMax),
                0f);
            popup.transform.position = jittered;
            popup.gameObject.SetActive(true);

            popup.Play(amount, type, Release);
        }

        private WorldDamagePopup CreatePopup()
        {
            Transform parent = poolRoot != null ? poolRoot : transform;
            return Instantiate(popupPrefab, parent);
        }

        private void Release(WorldDamagePopup popup)
        {
            popup.gameObject.SetActive(false);
            _pool.Push(popup);
        }
    }
}
