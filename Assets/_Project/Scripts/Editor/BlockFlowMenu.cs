using UnityEditor;
using UnityEngine;

public static class BlockFlowMenu
{
    private const string ReloadLevelItem = "BlockFlow/Reload Current Level %#r"; // Ctrl/Cmd + Shift + R

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
