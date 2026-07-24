namespace BounceHeroes.Core
{
    /// <summary>
    /// 게임에서 재생하는 사운드의 식별자입니다. <c>AudioDatabase</c>에서 FMOD 이벤트(EventReference) 또는 Key와 매핑됩니다.
    /// BGM/SFX를 한 enum에 함께 두고, 실제 라우팅(VCA/emitter)은 <c>AudioDatabase.Entry.bus</c>가 결정합니다.
    /// <para>
    /// 값은 카테고리별로 100 단위 구간을 나누고, 구간 내에서는 10씩 띄워 명시적으로 부여합니다.
    /// 새 항목을 추가할 때는 반드시 새 값을 명시적으로 지정하세요(값을 생략하면 앞 항목+1로 자동 배정되어
    /// 다른 항목과 충돌할 수 있습니다). 기존 값은 이미 SO 자산/세이브에 직렬화되어 있을 수 있으니 재배치하지 마세요.
    /// </para>
    /// </summary>
    public enum AudioId
    {
        None = 0,

        // --- BGM (100번대) ---
        BgmGameplay = 100,
        BgmResultWin = 110,
        BgmResultLose = 120,

        // --- SFX: UI (200번대) ---
        UiOpenPauseMenu = 200,
        UiClosePauseMenu = 210,
        UiClick = 220,
        UIOpenSelectSkillMenu = 230,
        UISelectSkillChoice = 240,
        UiOpen = 250,

        // --- SFX: 전투 (300번대) ---
        BallFire = 300,
        MonsterHit = 310,
        MonsterCrit = 320,
        MonsterKill = 330,
        WaveClear = 340,
        Explosion = 350,
        PlayerHit = 360,
        LevelUp = 370,
        

        // --- SFX: 액티브 스킬 타격 (400번대) ---
        SkillLaserShow = 380,
        SkillHitFireBall = 400,
        SkillHitIceBall = 410,
        SkillHitClusterBall = 420,
        SkillHitGhostBall = 430,

        // --- Snapshot (500번대) ---
        SnapshotPause = 500,
        
        // --- Misc (1000번대) ---
        Transition = 1000
    }
}
