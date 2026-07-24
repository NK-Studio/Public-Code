using BounceHeroes.Audio;
using BounceHeroes.Core;
using BounceHeroes.Leaderboard;
using BounceHeroes.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BounceHeroes.Bootstrap
{
    public sealed class IntroLifetimeScope : LifetimeScope
    {
        [SerializeField] private AudioDatabase audioDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<IAudioService>(AudioService.GetOrCreate(audioDatabase));
            builder.RegisterInstance(LeaderboardServiceFactory.Create());

            builder.RegisterComponentInHierarchy<IntroScreenController>();
        }
    }
}
