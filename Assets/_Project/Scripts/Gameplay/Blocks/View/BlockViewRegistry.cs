using System.Collections.Generic;

/// <summary>
/// Abstraction for the id → view lookup consumers depend on. Keeping callers
/// (drag, grinder, juice, ice-melt) bound to the interface instead of the
/// concrete registry lets tests substitute a fake and leaves room for
/// alternate impls (e.g. a weak-ref registry) without rippling changes.
/// </summary>
public interface IBlockViewRegistry
{
    int Count { get; }
    void Register(BlockId id, BlockView view);
    bool Unregister(BlockId id);
    bool TryGet(BlockId id, out BlockView view);
    void Clear();
}

/// <summary>
/// Central lookup from <see cref="BlockId"/> to the <see cref="BlockView"/>
/// currently representing it. Populated by the level builder as it
/// spawns views and queried by gameplay systems that need to reach the
/// visual representation of a logical block.
///
/// Deliberately kept separate from <see cref="IBlockViewFactory"/>: the
/// factory owns lifetime (pool in, pool out), the registry owns identity
/// (id → view). Keeping them split avoids bloating the factory's public API
/// with queries it doesn't need and keeps the registry trivially mockable.
/// </summary>
public sealed class BlockViewRegistry : IBlockViewRegistry
{
    private readonly Dictionary<BlockId, BlockView> views = new Dictionary<BlockId, BlockView>();

    public int Count => views.Count;

    public void Register(BlockId id, BlockView view)
    {
        if (!id.IsValid || view == null) return;
        views[id] = view;
    }

    public bool Unregister(BlockId id) => views.Remove(id);

    public bool TryGet(BlockId id, out BlockView view) => views.TryGetValue(id, out view);

    public void Clear() => views.Clear();
}
