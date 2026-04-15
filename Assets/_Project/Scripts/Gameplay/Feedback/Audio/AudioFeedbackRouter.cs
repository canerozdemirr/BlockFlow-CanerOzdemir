using System;
using System.Collections.Generic;
using VContainer.Unity;

// The ONLY place in the project that decides "this event -> this sound".
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
        subs.Add(bus.Subscribe<BlockGrindCompletedEvent>(_ => audio.Stop(SfxKey.BlockGround)));
        subs.Add(bus.Subscribe<BlockRevealedEvent>(_    => audio.Play(SfxKey.IceReveal)));

        // Win/Lose SFX fire when the outcome popup is visible, not when the
        // level technically ends — the win path has a 1s celebration delay
        // and playing the sting before the panel appears feels disconnected.
        subs.Add(bus.Subscribe<LevelOutcomePopupShownEvent>(e =>
            audio.Play(e.Won ? SfxKey.LevelWon : SfxKey.LevelLost)));
    }

    public void Dispose()
    {
        for (int i = 0; i < subs.Count; i++) subs[i].Dispose();
        subs.Clear();
    }
}
