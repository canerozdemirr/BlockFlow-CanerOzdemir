/// <summary>
/// Abstracts the platform pointer input (mouse or touch) behind a single
/// method that returns the current frame's <see cref="PointerState"/>. Using
/// an interface here keeps the DragController free of any direct dependency
/// on Unity's input APIs, which in turn makes the controller unit-testable
/// with a fake input service.
///
/// Implementations are expected to return <c>false</c> when no pointer is
/// available (e.g. no mouse connected, no touch screen active) so the drag
/// controller can skip its tick cleanly.
/// </summary>
public interface IInputService
{
    bool TryGetPointer(out PointerState state);
}
