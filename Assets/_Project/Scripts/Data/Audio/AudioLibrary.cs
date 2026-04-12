using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-authored mapping from <see cref="SfxKey"/> to the actual audio
/// clip, plus per-cue volume and pitch-jitter range. One asset shared by
/// the entire gameplay scope; drops into the <c>AudioLibrary</c> inspector
/// field on the <c>GameplayLifetimeScope</c>.
///
/// Lookup is lazy and rebuilt on <see cref="OnValidate"/> so in-editor edits
/// don't serve stale entries.
/// </summary>
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
    }

    [SerializeField, Tooltip("All audio cues the gameplay layer may play. Entries with null clips are ignored at runtime.")]
    private Entry[] entries;

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
