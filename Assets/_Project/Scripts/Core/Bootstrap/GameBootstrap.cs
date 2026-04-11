using UnityEngine;

namespace BlockFlow.Core.Bootstrap
{
    /// <summary>
    /// Runs once before the first scene loads. Applies mobile-friendly runtime
    /// settings that should be in effect for the whole lifetime of the app.
    /// Kept intentionally tiny: no DI, no allocations, no scene spawns.
    /// All dependency wiring happens through VContainer LifetimeScopes.
    /// </summary>
    public static class GameBootstrap
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            Input.multiTouchEnabled = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
