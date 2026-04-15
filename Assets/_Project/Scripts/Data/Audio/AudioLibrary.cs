using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "BlockFlow/Data/Audio Library", fileName = "AudioLibrary")]
public sealed class AudioLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public SfxKey key;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume;

        [Tooltip("Min/max pitch. Leave both at 1 for no jitter; spread them for per-play variation.")]
        public Vector2 pitchRange;

        [Min(0f), Tooltip("Seconds to skip from the start of the clip. 0 plays from the beginning. Use to trim dead air or a ramp-in baked into the clip.")]
        public float startOffset;

        [Min(0f), Tooltip("Minimum seconds between successive plays of this cue. Rapid-fire events (step, bump) stack multiple triggers in the same frame; this debounces them. 0 = no cooldown.")]
        public float minInterval;
    }

    [SerializeField, Tooltip("All audio cues the gameplay layer may play. Entries with null clips are ignored at runtime.")]
    private Entry[] entries;

    [SerializeField, Tooltip("Optional mixer group every SFX source is routed through. Lets you control SFX volume, duck under music, or apply bus effects without touching per-source volume. Leave null to play direct to the AudioListener.")]
    private AudioMixerGroup sfxMixerGroup;

    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

    private Dictionary<SfxKey, Entry> byKey;

    public bool TryGet(SfxKey key, out Entry result)
    {
        EnsureIndex();
        return byKey.TryGetValue(key, out result);
    }

    private void EnsureIndex()
    {
        if (byKey != null) return;
        byKey = new Dictionary<SfxKey, Entry>();
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e.key == SfxKey.None || e.clip == null) continue;
            byKey[e.key] = e;
        }
    }

    private void OnValidate() => byKey = null;
}
