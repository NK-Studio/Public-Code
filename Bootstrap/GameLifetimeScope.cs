using BounceHeroes.Audio;
using BounceHeroes.Core;
using BounceHeroes.Data;
using BounceHeroes.FX;
using BounceHeroes.Gameplay;
using BounceHeroes.Leaderboard;
using BounceHeroes.Managers;
using BounceHeroes.Score;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 씬 전역 DI 컨테이너의 진입점입니다.
/// 씬 매니저/시스템을 인터페이스로 등록하고, 데이터(FXDatabase·GameBalance)와 서비스(FX·Combat)를 주입 대상으로 등록합니다.
/// 소비자는 정적 <c>.Instance</c> 대신 이 등록물들을 [Inject]로 주입받습니다.
/// </summary>
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private FXDatabase fxDatabase;
    [SerializeField] private GameBalanceData gameBalance;
    [SerializeField] private AudioDatabase audioDatabase;

    protected override void Configure(IContainerBuilder builder)
    {
        // 데이터 SO / 서비스
        builder.RegisterInstance(fxDatabase);
        builder.RegisterInstance(gameBalance);
        builder.RegisterInstance<IAudioService>(AudioService.GetOrCreate(audioDatabase));
        builder.Register<IFXService, FXService>(Lifetime.Singleton);
        builder.Register<ICombatService, CombatService>(Lifetime.Singleton);
        builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
        builder.RegisterInstance<ILeaderboardService>(LeaderboardServiceFactory.Create());

        // 매니저는 인터페이스로 노출해 정적 참조 없이 주입받게 한다.
        builder.RegisterComponentInHierarchy<GameManager>().As<IGameFlow>();
        builder.RegisterComponentInHierarchy<SkillManager>().As<ISkillService>();

        // 나머지 씬 시스템(자체가 [Inject] 대상이거나 참조 대상)
        builder.RegisterComponentInHierarchy<WaveManager>();
        builder.RegisterComponentInHierarchy<JuiceManager>();
        builder.RegisterComponentInHierarchy<AudioManager>();
        builder.RegisterComponentInHierarchy<ScoreManager>();
        builder.RegisterComponentInHierarchy<WaveSpawnSequencer>();
        builder.RegisterComponentInHierarchy<UIManager>();
        builder.RegisterComponentInHierarchy<BallLauncher>();
        builder.RegisterComponentInHierarchy<GridField>();
        builder.RegisterComponentInHierarchy<WorldDamagePopupSpawner>();
        builder.RegisterComponentInHierarchy<PlayerAimPose>();
        builder.RegisterComponentInHierarchy<SkillSelectController>();
    }
}
