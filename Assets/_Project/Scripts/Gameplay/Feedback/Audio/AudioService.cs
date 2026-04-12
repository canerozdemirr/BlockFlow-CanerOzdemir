using System;
using UnityEngine;

/// <summary>
/// Central audio playback service. Owns a small round-robin ring of
/// <see cref="AudioSource"/> instances and plays clips by looking them up
/// in a designer-authored <see cref="AudioLibrary"/>.
///
/// <para>
/// Design notes:
/// <list type="bullet">
///   <item><b>Round-robin, not per-clip pooling.</b> The simulation never spams more than a handful of overlapping SFX, so a fixed ring of 8 sources is simpler and allocation-free per play.</item>
///   <item><b>Null-safe.</b> A missing library or missing entry no-ops. This lets the scope compile and run before any clips are authored.</item>
///   <item><b>Per-play pitch jitter.</b> Entries carry a <see cref="Vector2"/> pitch range; same min and max = no jitter.</item>
///   <item><b>Spatial blend = 0.</b> Every SFX is 2D; the puzzle is small enough that positional audio buys nothing.</item>
/// </list>
/// </para>
/// </summary>
public sealed class AudioService : IDisposable
{
    private const int SourceCount = 8;

    private readonly AudioLibrary library;
    private readonly AudioSource[] sources;
    private readonly GameObject root;
    private int next;

    public AudioService(AudioLibrary library)
    {
        this.library = library;

        root = new GameObject("[AudioService]");
        sources = new AudioSource[SourceCount];
        for (int i = 0; i < SourceCount; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(root.transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            sources[i] = src;
        }
    }

    /// <summary>
    /// Plays the cue mapped to <paramref name="key"/>. No-op on missing
    /// library, missing entry, or unassigned clip — the caller never needs
    /// a null check.
    /// </summary>
    public void Play(SfxKey key)
    {
        if (library == null) return;
        if (!library.TryGet(key, out var entry)) return;
        if (entry.clip == null) return;

        var src = sources[next];
        next = (next + 1) % sources.Length;

        float minPitch = entry.pitchRange.x;
        float maxPitch = entry.pitchRange.y;
        if (minPitch <= 0f && maxPitch <= 0f)
        {
            src.pitch = 1f;
        }
        else if (Mathf.Approximately(minPitch, maxPitch))
        {
            src.pitch = Mathf.Max(0.01f, minPitch);
        }
        else
        {
            src.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        }

        src.PlayOneShot(entry.clip, entry.volume > 0f ? entry.volume : 1f);
    }

    public void Dispose()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
    }
}
