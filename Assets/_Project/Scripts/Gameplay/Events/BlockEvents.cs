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

// Fired AFTER the view's overlay has been refreshed.
public readonly struct BlockRevealedEvent
{
    public readonly BlockId BlockId;

    public BlockRevealedEvent(BlockId id) { BlockId = id; }
}

public readonly struct BlockGrindCompletedEvent
{
    public readonly BlockId BlockId;

    public BlockGrindCompletedEvent(BlockId id) { BlockId = id; }
}

// Fired when the outcome popup becomes visible, decoupled from LevelWon/LostEvent
// so audio/haptics can align with the visual, not the logical end-of-level.
public readonly struct LevelOutcomePopupShownEvent
{
    public readonly bool Won;

    public LevelOutcomePopupShownEvent(bool won) { Won = won; }
}
