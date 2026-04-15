using UnityEngine;
using UnityEngine.InputSystem;

// Pointer.current auto-routes to whichever device is active (mouse in editor,
// touch on device).
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
