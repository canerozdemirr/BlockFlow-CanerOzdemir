using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlowManager
{
    private const string GameplaySceneName = "Gameplay";
    private static bool isLoading;

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
