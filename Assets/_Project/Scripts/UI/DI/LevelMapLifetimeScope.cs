using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using VContainer.Unity;

namespace BlockFlow.UI
{
    public class LevelMapLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelCatalog levelCatalog;

        [Header("Audio")]
        [SerializeField, Tooltip("Looping background track for the level map. Null = silence.")]
        private AudioClip mapMusic;

        [SerializeField, Range(0f, 1f), Tooltip("Playback volume for the map music track.")]
        private float mapMusicVolume = 0.5f;

        [SerializeField, Tooltip("Mixer group the music source routes through. Set to the Music group on GameAudioMixer so the Music toggle controls bus volume. Null = direct output (bool gate still works).")]
        private AudioMixerGroup musicMixerGroup;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register(c => new LevelProgressionService(levelCatalog, c.Resolve<ISaveRepository>()), Lifetime.Singleton);

            // Entry-pointed so scope Dispose (scene unload) stops the track —
            // no extra plumbing needed to cut music on scene switch.
            builder.RegisterEntryPoint<MusicService>()
                .WithParameter(mapMusic)
                .WithParameter(mapMusicVolume)
                .WithParameter(musicMixerGroup);
        }
    }
}
