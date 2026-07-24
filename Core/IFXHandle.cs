namespace BounceHeroes.Core
{
    /// <summary>
    /// 부착형(지속) 이펙트 인스턴스의 핸들입니다. 사용이 끝나면 <see cref="Release"/>로 풀에 반환합니다.
    /// </summary>
    public interface IFXHandle
    {
        /// <summary>이펙트를 멈추고 풀에 반환합니다. 여러 번 호출해도 안전합니다.</summary>
        void Release();
    }
}
