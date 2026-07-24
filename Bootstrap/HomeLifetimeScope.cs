using BounceHeroes.Audio;
using BounceHeroes.Core;
using BounceHeroes.Leaderboard;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class HomeLifetimeScope : LifetimeScope
{
    [SerializeField] private AudioDatabase audioDatabase;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance<IAudioService>(AudioService.GetOrCreate(audioDatabase));
        builder.RegisterInstance<ILeaderboardService>(LeaderboardServiceFactory.Create());

        builder.RegisterComponentInHierarchy<HomeController>();
        builder.RegisterComponentInHierarchy<HomeUIManager>();
    }
}
