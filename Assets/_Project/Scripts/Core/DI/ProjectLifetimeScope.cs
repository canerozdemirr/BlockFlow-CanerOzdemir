using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISceneLoader, SceneFlowLoader>(Lifetime.Singleton);
        builder.Register<IPrefabLoader, ResourcesPrefabLoader>(Lifetime.Singleton);
        builder.Register<ISaveRepository, PlayerPrefsSaveRepository>(Lifetime.Singleton);
    }
}
