using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>
/// Checks whether the board is clear after every successful grind. The
/// evaluator is intentionally tiny: one subscription, one post-ground
/// check, one publish. Win semantics live here and only here so the rule
/// can be swapped (e.g. "clear N blocks", "survive X seconds") without
/// touching the simulation or the UI.
/// </summary>
public sealed class WinConditionEvaluator : IStartable, IDisposable
{
    private readonly IEventBus bus;
    private readonly LevelContext context;
    private readonly GameStateService state;
    private readonly List<IDisposable> subs = new List<IDisposable>();

    public WinConditionEvaluator(IEventBus bus, LevelContext context, GameStateService state)
    {
        this.bus = bus;
        this.context = context;
        this.state = state;
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
        if (state.Current != GamePhase.Playing) return;
        if (context.Grid == null) return;
        if (context.Grid.BlockCount > 0) return;

        bus.Publish(new LevelWonEvent());
    }
}
