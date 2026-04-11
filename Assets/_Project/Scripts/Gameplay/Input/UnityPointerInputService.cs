using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Default <see cref="IInputService"/> backed by the new Input System's
/// <see cref="Pointer"/> abstraction. A single implementation covers both
/// mouse (editor) and touch (device) because <c>Pointer.current</c>
/// automatically routes to whichever device is active.
///
/// Edge detection (<see cref="PointerPhase.Began"/> / <see cref="PointerPhase.Ended"/>)
/// is derived from <c>press.wasPressedThisFrame</c> / <c>wasReleasedThisFrame</c>
/// so the drag controller doesn't have to maintain its own press-state
/// history.
/// </summary>
public sealed class UnityPointerInputService : IInputService
{
    public bool TryGetPointer(out PointerState state)
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            state = default;
            return false;
        }

        var screenPos = pointer.position.ReadValue();
        var press = pointer.press;

        PointerPhase phase;
        if (press.wasPressedThisFrame)
            phase = PointerPhase.Began;
        else if (press.wasReleasedThisFrame)
            phase = PointerPhase.Ended;
        else if (press.isPressed)
            phase = PointerPhase.Moved;
        else
            phase = PointerPhase.None;

        state = new PointerState(screenPos, phase);
        return true;
    }
}
