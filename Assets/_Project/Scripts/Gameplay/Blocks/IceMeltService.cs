using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Implements the "global grind counter" rule picked during planning: every
/// successful grind decrements the ice counter on every iced block. When a
/// block's counter reaches zero, its view's ice overlay is refreshed and a
/// <see cref="BlockRevealedEvent"/> goes out for Phase 7 VFX to hook.
///
/// Listens for <see cref="BlockGroundEvent"/> — the one event that fires on
/// every successful consumption — so there is no ambiguity about when ice
/// ticks.
/// </summary>
public sealed class IceMeltService : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly BlockViewRegistry viewRegistry;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    // Scratch list used to avoid mutating the blocks dictionary while iterating.
    private readonly List<BlockModel> scratch = new List<BlockModel>();

    public IceMeltService(IEventBus bus, LevelContext context, BlockViewRegistry viewRegistry)
    {
        this.bus = bus;
        this.context = context;
        this.viewRegistry = viewRegistry;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockGroundEvent>(OnBlockGround));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }

    private void OnBlockGround(BlockGroundEvent _)
    {
        if (context.Grid == null) return;

        // Copy current blocks into a scratch list so we iterate over a stable
        // snapshot even if future hooks mutate the grid indirectly.
        scratch.Clear();
        foreach (var pair in context.Grid.Blocks)
            scratch.Add(pair.Value);

        for (int i = 0; i < scratch.Count; i++)
        {
            var block = scratch[i];
            if (!block.IsIced) continue;

            int prevLevel = block.IceLevel;
            block.TickIce();

            if (viewRegistry.TryGet(block.Id, out var view) && view != null)
            {
                if (prevLevel > 0 && block.IceLevel == 0)
                {
                    // Fully revealed — hide overlay, spawn ice shatter particles
                    view.RefreshIceOverlay();
                    SpawnIceShatter(view.transform.position);
                    bus.Publish(new BlockRevealedEvent(block.Id));
                }
                else if (block.IsIced)
                {
                    // Still iced but level decreased — update text and opacity
                    view.RefreshIceOverlay();
                }
            }
        }

        scratch.Clear();
    }

    /// <summary>
    /// Spawns a burst of light blue/white ice shard particles at the block's position.
    /// </summary>
    private static void SpawnIceShatter(Vector3 worldPosition)
    {
        var go = new GameObject("IceShatter");
        go.transform.position = worldPosition + Vector3.up * 0.5f;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(0.75f, 0.9f, 1f, 0.9f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.8f, 0.92f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        renderer.material = new Material(shader);
        renderer.material.SetColor("_Color", new Color(0.75f, 0.9f, 1f));

        ps.Play();
    }
}
