using UnityEngine;

public readonly struct PointerState
{
    public readonly Vector2 ScreenPosition;
    public readonly PointerPhase Phase;

    public PointerState(Vector2 screenPosition, PointerPhase phase)
    {
        ScreenPosition = screenPosition;
        Phase = phase;
    }

    public bool IsBegan    => Phase == PointerPhase.Began;
    public bool IsMoved    => Phase == PointerPhase.Moved;
    public bool IsEnded    => Phase == PointerPhase.Ended;
    public bool IsCanceled => Phase == PointerPhase.Canceled;

    public bool IsEndOrCancel => Phase == PointerPhase.Ended || Phase == PointerPhase.Canceled;
}
