using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer;

/// <summary>
/// Scene-owned component that shakes its transform in response to impact
/// events on the bus. Attach to the gameplay camera; the
/// <c>GameplayCameraFitter</c> handles placement and this component handles
/// reactive motion, keeping the two responsibilities separate.
///
/// Shake is done with PrimeTween's <c>ShakeLocalPosition</c> — it caches the
/// starting position at tween start and restores it on completion, so
/// multiple back-to-back shakes don't drift the camera.
/// </summary>
public sealed class CameraShaker : MonoBehaviour
{
    [SerializeField, Tooltip("Shake strength when the player bumps the block into a wall or neighbor.")]
    private float bumpStrength = 0.15f;

    [SerializeField, Min(0.01f)]
    private float bumpDuration = 0.18f;

    [SerializeField, Tooltip("Shake strength when a block is successfully ground.")]
    private float groundStrength = 0.25f;

    [SerializeField, Min(0.01f)]
    private float groundDuration = 0.25f;

    private IEventBus bus;
    private readonly List<IDisposable> subs = new List<IDisposable>();
    private Tween activeShake;

    [Inject]
    public void Construct(IEventBus bus)
    {
        this.bus = bus;
        subs.Add(bus.Subscribe<BlockBumpedWallEvent>(_ => Shake(bumpStrength, bumpDuration)));
        subs.Add(bus.Subscribe<BlockGroundEvent>(_     => Shake(groundStrength, groundDuration)));
    }

    private void OnDestroy()
    {
        if (activeShake.isAlive) activeShake.Stop();
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    /// <summary>
    /// Starts a shake. If a previous shake is still running it is stopped
    /// first — otherwise PrimeTween's position caching gets confused and
    /// leaves the camera at the wrong base position.
    /// </summary>
    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;
        if (activeShake.isAlive) activeShake.Stop();
        activeShake = Tween.ShakeLocalPosition(transform, Vector3.one * strength, duration);
    }
}
