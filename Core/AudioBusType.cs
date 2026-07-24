namespace BounceHeroes.Core
{
    /// <summary>
    /// 사운드가 속한 논리 버스입니다. Key 모드에서 어느 KeyList를 조회할지, 그리고 어느 emitter로 재생할지를 결정합니다.
    /// FMOD Plus의 <c>AudioType</c>과는 별개의 게임측 enum이며, 서비스 계층에서 변환합니다.
    /// </summary>
    public enum AudioBusType
    {
        /// <summary>배경 음악(지속/루프).</summary>
        Bgm,

        /// <summary>효과음(일회성).</summary>
        Sfx,

        /// <summary>환경음(앰비언트).</summary>
        Amb,
    }
}
