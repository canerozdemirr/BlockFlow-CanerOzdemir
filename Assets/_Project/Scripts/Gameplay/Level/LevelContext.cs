using UnityEngine;

// Callers must check IsActive before reading Grid/GridRoot.
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
