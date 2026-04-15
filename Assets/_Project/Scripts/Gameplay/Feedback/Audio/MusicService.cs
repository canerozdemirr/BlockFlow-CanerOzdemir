using System;
using UnityEngine;
using UnityEngine.Audio;
using VContainer.Unity;

// Gameplay loads additively on top of Level Map, so the map's scope never tears
// down while gameplay is running. To avoid both tracks playing at once, Start
// pushes onto a static stack and pauses the previous top; Dispose pops and
// resumes whatever was underneath.
public sealed class MusicService : IStartable, ITickable, IDisposable
{
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
        // Share the mixer with AudioService so one flip of MusicEnabled mutes the
        // whole bus regardless of which scope came up first.
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

        if (stack.Count > 0) stack.Peek().PauseUnderneath();
        stack.Push(this);

        wasEnabled = AudioService.MusicEnabled;
        if (track != null && wasEnabled) source.Play();
    }

    public void Tick()
    {
        if (source == null || track == null) return;

        // Only the top-of-stack responds to the mute toggle — ones underneath
        // are intentionally paused because another scene owns playback.
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
