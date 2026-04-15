// Checked BEFORE the grid collision check — filters out illegal directions per block.
public interface IMovementStrategy
{
    bool CanMove(BlockModel block, GridDirection direction);
}
