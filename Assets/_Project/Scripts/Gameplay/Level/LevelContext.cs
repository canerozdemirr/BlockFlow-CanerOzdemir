using UnityEngine;

/// <summary>
/// Shared runtime handle onto the currently loaded level. Holds the pieces
/// every per-level system needs — the authoritative <see cref="GridModel"/>,
/// the scene root every view is parented under — so the drag controller,
/// win/lose evaluators, debug tools, and anything else can grab them through
/// DI without each caller knowing how the level was assembled.
///
/// The level builder (Phase 5) calls <see cref="Bind"/> right after building
/// a level and <see cref="Clear"/> when tearing it down. Callers must check
/// <see cref="IsActive"/> before reading the other properties.
/// </summary>
public sealed class LevelContext
{
    public GridModel Grid { get; private set; }
    public Transform GridRoot { get; private set; }

    public bool IsActive => Grid != null;

    public void Bind(GridModel grid, Transform gridRoot)
    {
        Grid = grid;
        GridRoot = gridRoot;
    }

    public void Clear()
    {
        Grid = null;
        GridRoot = null;
    }
}
