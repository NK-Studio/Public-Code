using UnityEngine;

namespace BounceHeroes.Core
{
    /// <summary>
    /// 사운드를 재생·제어하는 서비스입니다.
    /// "어떤 id가 어떤 FMOD 이벤트/Key인지"는 <c>AudioDatabase</c>가, "무엇을 어떻게 울릴지"는 이 서비스가 담당하며,
    /// "언제 울릴지"는 이벤트 허브를 구독하는 상위 계층(<c>AudioManager</c>)이 결정합니다.
    /// 소비자는 정적 <c>.Instance</c> 대신 이 인터페이스를 [Inject]로 주입받습니다.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>효과음을 일회성으로 재생합니다. 지정 위치에서 재생되며 자동으로 해제됩니다.</summary>
        void PlaySfx(AudioId id, Vector3 position = default);

        /// <summary>배경 음악을 재생합니다. 이미 같은 곡이 재생 중이면 무시합니다.</summary>
        void PlayBgm(AudioId id);

        /// <summary>현재 재생 상태와 관계없이 배경 음악을 처음부터 다시 재생합니다.</summary>
        void RestartBgm(AudioId id);

        /// <summary>배경 음악을 정지합니다.</summary>
        /// <param name="fade">true면 페이드아웃하며 정지합니다.</param>
        void StopBgm(bool fade = true);

        /// <summary>
        /// 현재 재생 중인 BGM 이벤트에 FMOD 파라미터 값을 설정합니다.
        /// (예: 플레이어 사망 시 <c>event:/BGM/Main</c>의 "Dead" 파라미터를 1로 설정해 음악을 전환)
        /// BGM이 재생 중이 아니면 무시됩니다.
        /// </summary>
        void SetBgmParameter(string parameterName, float value);

        /// <summary>
        /// FMOD 스냅샷을 재생합니다. (예: 일시정지 중 BGM 볼륨을 낮추는 "Pause" 스냅샷)
        /// 이미 같은 스냅샷이 재생 중이면 무시합니다.
        /// </summary>
        void PlaySnapshot(AudioId id);

        /// <summary>재생 중인 스냅샷을 정지합니다.</summary>
        /// <param name="fade">true면 페이드아웃하며 정지합니다.</param>
        void StopSnapshot(bool fade = true);

        /// <summary>BGM 버스(VCA)의 켬/끔을 설정합니다.</summary>
        void SetBgmEnabled(bool enabled);

        /// <summary>SFX 버스(VCA)의 켬/끔을 설정합니다.</summary>
        void SetSfxEnabled(bool enabled);
    }
}
