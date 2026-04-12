using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>
/// Translates gameplay bus events into <see cref="AudioService.Play"/>
/// calls. Keeping the routing in its own tiny entry point means:
///
/// <list type="bullet">
///   <item>The audio service knows nothing about gameplay events.</item>
///   <item>The gameplay code knows nothing about audio.</item>
///   <item>Swapping the table is a one-file change.</item>
/// </list>
///
/// Every mapping lives here — the ONLY place in the project that decides
/// "this event → this sound".
/// </summary>
public sealed class AudioFeedbackRouter : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly AudioService audio;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public AudioFeedbackRouter(IEventBus bus, AudioService audio)
    {
        this.bus = bus;
        this.audio = audio;
    }

    public void Start()
    {
        subs.Add(bus.Subscribe<BlockDragStartedEvent>(_ => audio.Play(SfxKey.DragStart)));
        subs.Add(bus.Subscribe<BlockDragEndedEvent>(_   => audio.Play(SfxKey.DragEnd)));
        subs.Add(bus.Subscribe<BlockSteppedEvent>(_     => audio.Play(SfxKey.BlockStep)));
        subs.Add(bus.Subscribe<BlockBumpedWallEvent>(_  => audio.Play(SfxKey.WallBump)));
        subs.Add(bus.Subscribe<BlockGroundEvent>(_      => audio.Play(SfxKey.BlockGround)));
        subs.Add(bus.Subscribe<BlockRevealedEvent>(_    => audio.Play(SfxKey.IceReveal)));
        subs.Add(bus.Subscribe<LevelWonEvent>(_         => audio.Play(SfxKey.LevelWon)));
        subs.Add(bus.Subscribe<LevelLostEvent>(_        => audio.Play(SfxKey.LevelLost)));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }
}
