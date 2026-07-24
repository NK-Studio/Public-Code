namespace BounceHeroes.Core
{
    /// <summary>
    /// 전역(공용) 이펙트의 식별자입니다. <c>FXDatabase</c>에서 프리팹과 매핑됩니다.
    /// 볼별로 달라지는 타격 FX처럼 특정 오브젝트가 소유하는 이펙트는 여기 넣지 않고
    /// 프리팹 참조를 그대로 소유한 채 <c>IFXService.Play(prefab, ...)</c>로 풀링합니다.
    /// </summary>
    public enum FXId
    {
        None = 0,
        BallSpawner,
        Explosion,
        Electricity,
        PlayerHit,
        MonsterBurn,
        MonsterFreeze,
    }
}
