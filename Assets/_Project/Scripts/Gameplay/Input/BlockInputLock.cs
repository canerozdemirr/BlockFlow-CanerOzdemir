using System.Collections.Generic;

/// <summary>
/// Tracks blocks that are currently ineligible for drag input (e.g. being
/// auto-fed into a grinder). Prevents the player from re-grabbing a block
/// mid-animation and desyncing model vs view.
/// Cleared between levels.
/// </summary>
public sealed class BlockInputLock
{
    private readonly HashSet<BlockId> locked = new HashSet<BlockId>();

    public bool IsLocked(BlockId id) => locked.Contains(id);
    public void Lock(BlockId id) => locked.Add(id);
    public void Unlock(BlockId id) => locked.Remove(id);
    public void Clear() => locked.Clear();
}
