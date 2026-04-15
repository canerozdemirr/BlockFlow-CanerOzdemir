using System.Collections.Generic;

public interface IBlockViewRegistry
{
    int Count { get; }
    void Register(BlockId id, BlockView view);
    bool Unregister(BlockId id);
    bool TryGet(BlockId id, out BlockView view);
    void Clear();
}

// Factory owns lifetime; registry owns identity (id → view). Split keeps each mockable.
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
