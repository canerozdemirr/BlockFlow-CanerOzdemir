/// <summary>
/// Decides whether a given direction is legal for a specific block BEFORE the
/// grid collision check runs. Concrete strategies translate the block's
/// authored modifiers (axis locks today, anything else later) into a simple
/// yes/no filter that the input controller can use to ignore illegal drags.
///
/// Kept intentionally narrow so extra movement rules (jump, teleport,
/// ice-sliding) can plug in as separate implementations without reshaping the
/// call site.
/// </summary>
public interface IMovementStrategy
{
    bool CanMove(BlockModel block, GridDirection direction);
}
