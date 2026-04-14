using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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

    // PlayerPrefs keys for the two settings toggles. Persisted here (not in
    // a separate save repo) because they're global device prefs, not tied
    // to progression. Defaults to "on" for both on first launch.
    private const string SfxPrefKey   = "audio.sfx.enabled";
    private const string MusicPrefKey = "audio.music.enabled";

    // Exposed parameter names on the shared AudioMixer (see GameAudioMixer.mixer).
    // Flipping these to -80 dB hard-mutes the whole bus, including any source
    // still playing — cheaper and smoother than stopping sources by hand, and
    // it composes with future ducking (lower SFX -3 dB while music stingers play).
    internal const string SfxVolParam   = "SfxVol";
    internal const string MusicVolParam = "MusicVol";
    private const float MutedDb   = -80f;
    private const float UnmutedDb = 0f;

    private static AudioMixer sharedMixer;

    private static bool sfxEnabled   = PlayerPrefs.GetInt(SfxPrefKey,   1) == 1;
    private static bool musicEnabled = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;

    /// <summary>
    /// Gate for all SFX playback. When false, <see cref="Play"/> no-ops.
    /// Bound to the Sound toggle in the pause panel. Persists across
    /// sessions via PlayerPrefs.
    /// </summary>
    public static bool SfxEnabled
    {
        get => sfxEnabled;
        set
        {
            if (sfxEnabled == value) return;
            sfxEnabled = value;
            PlayerPrefs.SetInt(SfxPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplySfxMute();
        }
    }

    /// <summary>
    /// Gate for future background music. Currently just stores the flag —
    /// no music system is wired yet, so flipping this is a no-op audibly.
    /// Kept as a public static so the settings UI can bind today and the
    /// eventual music service can read the flag without another round-trip.
    /// </summary>
    public static bool MusicEnabled
    {
        get => musicEnabled;
        set
        {
            if (musicEnabled == value) return;
            musicEnabled = value;
            PlayerPrefs.SetInt(MusicPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusicMute();
        }
    }

    // Push the current bool state onto the mixer. Called when the flag flips
    // and once at construction so a newly loaded scene inherits the saved
    // mute state without waiting for the next toggle.
    private static void ApplySfxMute()
    {
        if (sharedMixer == null) return;
        sharedMixer.SetFloat(SfxVolParam, sfxEnabled ? UnmutedDb : MutedDb);
    }

    private static void ApplyMusicMute()
    {
        if (sharedMixer == null) return;
        sharedMixer.SetFloat(MusicVolParam, musicEnabled ? UnmutedDb : MutedDb);
    }

    // Called by AudioService / MusicService on construction. First caller wins;
    // we assume one mixer for the whole app (the library's sfxMixerGroup.audioMixer
    // and the MusicService's group.audioMixer should be the same asset).
    internal static void RegisterMixer(AudioMixer mixer)
    {
        if (mixer == null || sharedMixer == mixer) return;
        sharedMixer = mixer;
        ApplySfxMute();
        ApplyMusicMute();
    }

    private readonly AudioLibrary library;
    private readonly AudioSource[] sources;
    private readonly GameObject root;
    private readonly Dictionary<SfxKey, AudioSource> playing = new Dictionary<SfxKey, AudioSource>();
    // Per-key last-played timestamp (unscaled) used to honor Entry.minInterval.
    // Debounces rapid-fire events like BlockStepped that can fire multiple
    // times per frame during a slide and stack identical one-shots.
    private readonly Dictionary<SfxKey, float> lastPlayedAt = new Dictionary<SfxKey, float>();
    private int next;

    public AudioService(AudioLibrary library)
    {
        this.library = library;

        var mixerGroup = library != null ? library.SfxMixerGroup : null;
        if (mixerGroup != null) RegisterMixer(mixerGroup.audioMixer);

        root = new GameObject("[AudioService]");
        sources = new AudioSource[SourceCount];
        for (int i = 0; i < SourceCount; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(root.transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            // Skip the FX graph for our 2D UI-ish cues; tiny perf win, zero
            // behavioral cost because we don't use reverb zones on the map.
            src.bypassEffects = true;
            src.bypassListenerEffects = true;
            src.bypassReverbZones = true;
            // Route through the shared SFX mixer group when one is authored
            // so designers can control SFX bus volume / duck under music.
            if (mixerGroup != null) src.outputAudioMixerGroup = mixerGroup;
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
        if (!sfxEnabled) return;
        if (library == null) return;
        if (!library.TryGet(key, out var entry)) return;
        if (entry.clip == null) return;

        // Per-key cooldown. Events like BlockSteppedEvent fire per-cell during
        // a slide — without this gate a long slide stacks 5+ identical clips
        // on top of each other, causing phasing and burning voices. Uses
        // unscaledTime so a paused game doesn't break the next play.
        if (entry.minInterval > 0f)
        {
            float now = Time.unscaledTime;
            if (lastPlayedAt.TryGetValue(key, out var last) && now - last < entry.minInterval) return;
            lastPlayedAt[key] = now;
        }

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

        float volume = entry.volume > 0f ? entry.volume : 1f;

        // PlayOneShot ignores AudioSource.time, so when a startOffset is
        // authored we fall back to the clip/time/Play path. This occupies
        // the source until the clip (minus the offset) finishes, but with
        // 8 sources in the ring that's a non-issue for short cues.
        if (entry.startOffset > 0f && entry.startOffset < entry.clip.length)
        {
            src.clip = entry.clip;
            src.volume = volume;
            src.time = entry.startOffset;
            src.Play();
        }
        else
        {
            src.PlayOneShot(entry.clip, volume);
        }

        // Track the latest source used for this cue so Stop(key) can cut it.
        // AudioSource.Stop() stops both Play()-scheduled clips and PlayOneShot
        // overlays on the same source, so this works for both paths.
        playing[key] = src;
    }

    /// <summary>
    /// Stops the most recent play of <paramref name="key"/> if it's still
    /// playing. No-op otherwise. Used to cut cues that outlive the action
    /// that triggered them (e.g. the grinder SFX when the consume tween ends).
    /// </summary>
    public void Stop(SfxKey key)
    {
        if (!playing.TryGetValue(key, out var src)) return;
        if (src != null && src.isPlaying) src.Stop();
        playing.Remove(key);
    }

    public void Dispose()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
    }
}
