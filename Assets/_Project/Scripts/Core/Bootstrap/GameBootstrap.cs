using UnityEngine;

namespace BlockFlow.Core.Bootstrap
{
    // Runs once before the first scene loads. All DI wiring happens in VContainer LifetimeScopes.
    public static class GameBootstrap
    {
        private const int TargetFrameRate = 120;

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
