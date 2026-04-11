using UnityEditor;
using UnityEngine;

/// <summary>
/// Top-level "BlockFlow" menu for in-editor developer tooling. Deliberately
/// kept small: only commands that speed up the day-to-day iteration loop
/// belong here.
/// </summary>
public static class BlockFlowMenu
{
    private const string ReloadLevelItem = "BlockFlow/Reload Current Level %#r"; // Ctrl/Cmd + Shift + R

    /// <summary>
    /// Rebuilds the currently loaded level. Only useful in play mode because
    /// the <see cref="LevelRunner"/> and its service graph only exist while
    /// the scene is running.
    /// </summary>
    [MenuItem(ReloadLevelItem)]
    private static void ReloadCurrentLevel()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "BlockFlow",
                "Reload Current Level only works while the game is running in play mode.",
                "OK");
            return;
        }

        var bootstrapper = Object.FindFirstObjectByType<GameplayBootstrapper>();
        if (bootstrapper == null)
        {
            Debug.LogError("[BlockFlowMenu] No GameplayBootstrapper found in the active scene.");
            return;
        }

        bootstrapper.ReloadCurrent();
    }

    [MenuItem(ReloadLevelItem, true)]
    private static bool ReloadCurrentLevelValidate() => Application.isPlaying;
}
