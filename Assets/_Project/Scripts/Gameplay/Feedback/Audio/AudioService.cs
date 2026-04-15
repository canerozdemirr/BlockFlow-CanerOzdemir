using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioService : IDisposable
{
    private const int SourceCount = 8;

    private const string SfxPrefKey   = "audio.sfx.enabled";
    private const string MusicPrefKey = "audio.music.enabled";

    // Flipping these mixer params to -80 dB hard-mutes the whole bus, including
    // any source still playing — cheaper than stopping sources by hand and it
    // composes with future ducking.
    internal const string SfxVolParam   = "SfxVol";
    internal const string MusicVolParam = "MusicVol";
    private const float MutedDb   = -80f;
    private const float UnmutedDb = 0f;

    private static AudioMixer sharedMixer;

    private static bool sfxEnabled   = PlayerPrefs.GetInt(SfxPrefKey,   1) == 1;
    private static bool musicEnabled = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;

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

    // First caller wins; we assume one mixer for the whole app.
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
            src.bypassEffects = true;
            src.bypassListenerEffects = true;
            src.bypassReverbZones = true;
            if (mixerGroup != null) src.outputAudioMixerGroup = mixerGroup;
            sources[i] = src;
        }
    }

    public void Play(SfxKey key)
    {
        if (!sfxEnabled) return;
        if (library == null) return;
        if (!library.TryGet(key, out var entry)) return;
        if (entry.clip == null) return;

        // Per-key cooldown. BlockSteppedEvent fires per-cell during a slide — without
        // this gate a long slide stacks identical clips and causes phasing. Uses
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

        // PlayOneShot ignores AudioSource.time, so startOffset forces the clip/Play path.
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

        playing[key] = src;
    }

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
