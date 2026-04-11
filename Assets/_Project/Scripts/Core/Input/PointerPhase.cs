/// <summary>
/// Lifecycle phase of a pointer (mouse or touch) for a single frame. Used by
/// <see cref="IInputService"/> so the drag controller can react to press-down
/// and release edges without polling the platform APIs directly.
/// </summary>
public enum PointerPhase
{
    None,
    Began,
    Moved,
    Ended,
    Canceled
}
