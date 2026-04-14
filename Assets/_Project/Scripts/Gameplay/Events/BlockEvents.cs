/// <summary>
/// Fired by <see cref="GrinderService"/> after a block has been consumed.
/// Listeners:
/// <list type="bullet">
///   <item><see cref="IceMeltService"/> — decrements the global ice counter on every iced block.</item>
///   <item><see cref="WinConditionEvaluator"/> — checks for board clear.</item>
///   <item>Phase 7 audio / VFX — grinder SFX, particles.</item>
/// </list>
/// </summary>
public readonly struct BlockGroundEvent
{
    public readonly BlockId BlockId;
    public readonly int GrinderId;
    public readonly string ColorId;

    public BlockGroundEvent(BlockId blockId, int grinderId, string colorId)
    {
        BlockId = blockId;
        GrinderId = grinderId;
        ColorId = colorId;
    }
}

/// <summary>
/// Fired by <see cref="IceMeltService"/> when a block's ice counter reaches
/// zero. The view has already refreshed its overlay by the time this fires.
/// Phase 7 hooks this for the ice-shatter VFX.
/// </summary>
public readonly struct BlockRevealedEvent
{
    public readonly BlockId BlockId;

    public BlockRevealedEvent(BlockId id) { BlockId = id; }
}

/// <summary>
/// Fired by <see cref="GrinderService"/> when a consumed block's dismiss
/// tween finishes, i.e. the grind animation is visually over. Audio hooks
/// this to cut the grinder SFX so it doesn't outlive the motion.
/// </summary>
public readonly struct BlockGrindCompletedEvent
{
    public readonly BlockId BlockId;

    public BlockGrindCompletedEvent(BlockId id) { BlockId = id; }
}

/// <summary>
/// Fired by the outcome popup view at the moment the panel becomes visible
/// (after the win-celebration delay, or immediately on lose). Decouples
/// "the level ended" (<see cref="LevelWonEvent"/>/<see cref="LevelLostEvent"/>)
/// from "the popup is on screen" so audio/haptics can align with the visual.
/// </summary>
public readonly struct LevelOutcomePopupShownEvent
{
    public readonly bool Won;

    public LevelOutcomePopupShownEvent(bool won) { Won = won; }
}
