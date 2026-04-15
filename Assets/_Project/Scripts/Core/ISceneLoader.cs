using System;

public interface ISceneLoader
{
    bool IsLoading { get; }
    bool IsGameplayLoaded { get; }

    event Action OnGameplayUnloaded;

    void LoadGameplay(Action onComplete = null);
    void UnloadGameplay(Action onComplete = null);
}

public sealed class SceneFlowLoader : ISceneLoader
{
    public bool IsLoading => SceneFlowManager.IsLoading;
    public bool IsGameplayLoaded => SceneFlowManager.IsGameplayLoaded;

    public event Action OnGameplayUnloaded
    {
        add    => SceneFlowManager.OnGameplayUnloaded += value;
        remove => SceneFlowManager.OnGameplayUnloaded -= value;
    }

    public void LoadGameplay(Action onComplete = null) => SceneFlowManager.LoadGameplay(onComplete);
    public void UnloadGameplay(Action onComplete = null) => SceneFlowManager.UnloadGameplay(onComplete);
}
