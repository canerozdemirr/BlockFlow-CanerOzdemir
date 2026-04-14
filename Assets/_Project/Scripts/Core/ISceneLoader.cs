using System;

/// <summary>
/// Abstraction over the gameplay scene lifecycle so UI screens don't depend
/// on the concrete <see cref="SceneFlowManager"/> static. Tests and alternate
/// flows (editor harness, smoke bot) can substitute a fake implementation.
/// </summary>
public interface ISceneLoader
{
    bool IsLoading { get; }
    bool IsGameplayLoaded { get; }

    event Action OnGameplayUnloaded;

    void LoadGameplay(Action onComplete = null);
    void UnloadGameplay(Action onComplete = null);
}

/// <summary>
/// Default implementation: delegates to the existing <see cref="SceneFlowManager"/>
/// static. Registered at the project scope so every screen resolves the same
/// instance.
/// </summary>
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
