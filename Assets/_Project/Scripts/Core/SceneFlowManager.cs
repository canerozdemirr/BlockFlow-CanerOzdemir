using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static utility for managing scene transitions between Level Map and Gameplay.
/// Fires <see cref="OnGameplayUnloaded"/> so listeners (like LevelMapScreen)
/// can refresh without a cross-assembly reference.
/// </summary>
public static class SceneFlowManager
{
    private const string GameplaySceneName = "Gameplay";
    private static bool isLoading;

    /// <summary>Fired after the Gameplay scene finishes unloading.</summary>
    public static event Action OnGameplayUnloaded;

    public static bool IsLoading => isLoading;

    public static bool IsGameplayLoaded
    {
        get
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == GameplaySceneName)
                    return true;
            }
            return false;
        }
    }

    public static void LoadGameplay(Action onComplete = null)
    {
        if (isLoading || IsGameplayLoaded)
        {
            onComplete?.Invoke();
            return;
        }

        isLoading = true;
        var op = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError("[SceneFlowManager] Failed to load Gameplay scene.");
            isLoading = false;
            return;
        }

        op.completed += _ =>
        {
            isLoading = false;
            var gameplayScene = SceneManager.GetSceneByName(GameplaySceneName);
            if (gameplayScene.isLoaded)
                SceneManager.SetActiveScene(gameplayScene);
            onComplete?.Invoke();
        };
    }

    public static void UnloadGameplay(Action onComplete = null)
    {
        if (isLoading || !IsGameplayLoaded)
        {
            onComplete?.Invoke();
            return;
        }

        isLoading = true;
        var op = SceneManager.UnloadSceneAsync(GameplaySceneName);
        if (op == null)
        {
            Debug.LogError("[SceneFlowManager] Failed to unload Gameplay scene.");
            isLoading = false;
            onComplete?.Invoke();
            return;
        }

        op.completed += _ =>
        {
            isLoading = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name != GameplaySceneName && scene.isLoaded)
                {
                    SceneManager.SetActiveScene(scene);
                    break;
                }
            }

            OnGameplayUnloaded?.Invoke();
            onComplete?.Invoke();
        };
    }
}
