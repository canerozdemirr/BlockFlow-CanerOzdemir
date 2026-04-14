using System;
using UnityEngine;
using UnityEngine.Audio;
using VContainer.Unity;

/// <summary>
/// Plays a single looping background track for the lifetime of its DI scope.
/// Each scene (Gameplay, Level Map) registers its own <see cref="MusicService"/>
/// with the track it wants.
///
/// <para>
/// <b>Scene-swap behavior.</b> The app loads Gameplay additively on top of
/// Level Map, so the map's scope never tears down while gameplay is running.
/// To avoid both tracks playing at once, <see cref="Start"/> pushes itself
/// onto a static stack and <i>pauses</i> the previous active service. When
/// this service's scope disposes (scene unload), <see cref="Dispose"/> pops
/// the stack and resumes whatever was underneath. Net effect: entering a
/// scene always silences the track beneath it, and exiting always restores
/// the one that was paused. No global coordinator, no cross-scene wiring.
/// </para>
///
/// <para>
/// Respects <see cref="AudioService.MusicEnabled"/>. The service ticks every
/// frame to notice the flag flipping (from the pause menu) and resumes or
/// pauses playback without restarting the clip.
/// </para>
///
/// <para>
/// Null track is allowed: the service still registers and still pushes onto
/// the stack (so it silences the one below), it just never plays anything
/// itself. This lets designers leave the field empty during early bring-up.
/// </para>
/// </summary>
public sealed class MusicService : IStartable, ITickable, IDisposable
{
    // Stack of active music services. Top of stack is the one that should be
    // audible. Using a stack (not a single "current") lets rapid scene swaps
    // or nested scopes unwind correctly.
    private static readonly System.Collections.Generic.Stack<MusicService> stack
        = new System.Collections.Generic.Stack<MusicService>();

    private readonly AudioClip track;
    private readonly float volume;
    private readonly AudioMixerGroup mixerGroup;
    private AudioSource source;
    private GameObject root;
    private bool wasEnabled;

    public MusicService(AudioClip track, float volume = 0.5f, AudioMixerGroup mixerGroup = null)
    {
        this.track = track;
        this.volume = Mathf.Clamp01(volume);
        this.mixerGroup = mixerGroup;
        // Share the mixer with AudioService so one flip of MusicEnabled mutes
        // the whole bus, regardless of which scope came up first.
        if (mixerGroup != null) AudioService.RegisterMixer(mixerGroup.audioMixer);
    }

    public void Start()
    {
        root = new GameObject("[MusicService]");
        UnityEngine.Object.DontDestroyOnLoad(root);
        source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.clip = track;
        if (mixerGroup != null) source.outputAudioMixerGroup = mixerGroup;

        // Silence whatever was playing so we don't stack tracks on top of each
        // other when Gameplay loads additively over Level Map.
        if (stack.Count > 0) stack.Peek().PauseUnderneath();
        stack.Push(this);

        wasEnabled = AudioService.MusicEnabled;
        if (track != null && wasEnabled) source.Play();
    }

    public void Tick()
    {
        if (source == null || track == null) return;

        // Only the top-of-stack service responds to the mute toggle — the
        // ones underneath are intentionally paused because another scene
        // owns playback right now.
        if (stack.Count == 0 || stack.Peek() != this) return;

        bool enabledNow = AudioService.MusicEnabled;
        if (enabledNow == wasEnabled) return;

        if (enabledNow)
        {
            source.UnPause();
            if (!source.isPlaying) source.Play();
        }
        else
        {
            source.Pause();
        }
        wasEnabled = enabledNow;
    }

    public void Dispose()
    {
        if (source != null) source.Stop();
        if (root != null) UnityEngine.Object.Destroy(root);
        source = null;
        root = null;

        // Pop ourselves off the stack wherever we are (normally the top, but
        // be defensive in case of odd teardown orders) and let the new top
        // resume.
        RemoveFromStack(this);
        if (stack.Count > 0) stack.Peek().ResumeFromUnderneath();
    }

    private void PauseUnderneath()
    {
        if (source != null && source.isPlaying) source.Pause();
    }

    private void ResumeFromUnderneath()
    {
        if (source == null || track == null) return;
        wasEnabled = AudioService.MusicEnabled;
        if (!wasEnabled) return;
        source.UnPause();
        if (!source.isPlaying) source.Play();
    }

    private static void RemoveFromStack(MusicService svc)
    {
        if (stack.Count == 0) return;
        if (stack.Peek() == svc) { stack.Pop(); return; }

        // Rare path: the popping instance isn't on top. Rebuild without it.
        var tmp = new System.Collections.Generic.List<MusicService>(stack);
        stack.Clear();
        for (int i = tmp.Count - 1; i >= 0; i--)
            if (tmp[i] != svc) stack.Push(tmp[i]);
    }
}
