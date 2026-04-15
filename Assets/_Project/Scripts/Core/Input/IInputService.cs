// Implementations return false when no pointer is available so callers skip cleanly.
public interface IInputService
{
    bool TryGetPointer(out PointerState state);
}
