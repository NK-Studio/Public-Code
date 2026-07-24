using UnityEngine;

namespace BounceHeroes.Core
{
    /// <summary>
    /// 이펙트를 오브젝트 풀에서 꺼내 재생하는 서비스입니다.
    /// "몇 개 미리 만들지·어느 프리팹인지·수명은 얼마인지"는 <c>FXDatabase</c>와 호출자가 정하고,
    /// 이 서비스는 재생과 반환(풀링)만 담당합니다.
    /// </summary>
    public interface IFXService
    {
        /// <summary>전역 이펙트를 지정 위치에 일회성으로 재생합니다. 수명 후 자동 반환됩니다.</summary>
        void Play(FXId id, Vector3 position);

        /// <summary>전역 이펙트를 회전·스케일과 함께 일회성으로 재생합니다.</summary>
        void Play(FXId id, Vector3 position, Quaternion rotation, float scale);

        /// <summary>
        /// 특정 오브젝트가 소유한 프리팹(예: 볼별 타격 FX)을 일회성으로 재생합니다.
        /// 프리팹별로 풀이 자동 생성됩니다.
        /// </summary>
        void Play(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float lifetime);

        /// <summary>부착형(지속) 이펙트를 <paramref name="parent"/> 자식으로 재생하고 핸들을 반환합니다.</summary>
        IFXHandle PlayAttached(FXId id, Transform parent);
    }
}
