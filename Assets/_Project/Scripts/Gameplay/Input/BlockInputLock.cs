using System.Collections.Generic;

// Prevents re-grabbing a block mid-animation (e.g. during auto-consume) which
// would desync model vs view.
public sealed class BlockInputLock
{
    private readonly HashSet<BlockId> locked = new HashSet<BlockId>();

    public bool IsLocked(BlockId id) => locked.Contains(id);
    public void Lock(BlockId id) => locked.Add(id);
    public void Unlock(BlockId id) => locked.Remove(id);
    public void Clear() => locked.Clear();
}
