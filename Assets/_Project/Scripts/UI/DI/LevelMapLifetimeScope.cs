using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BlockFlow.UI
{
    /// <summary>
    /// DI scope for the Level Map scene. Registers <see cref="LevelProgressionService"/>
    /// so the map screen can read/write player progression.
    /// </summary>
    public class LevelMapLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelCatalog levelCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register(_ => new LevelProgressionService(levelCatalog), Lifetime.Singleton);
        }
    }
}
