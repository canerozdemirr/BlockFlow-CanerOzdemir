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
