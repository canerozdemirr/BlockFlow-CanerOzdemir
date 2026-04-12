using UnityEngine;

/// <summary>
/// Tracks the player's position in a <see cref="LevelCatalog"/> and
/// persists the current index to <see cref="PlayerPrefs"/> so progress
/// survives app restarts.
///
/// Deliberately minimal: no star ratings, no per-level completion flags,
/// no best-time tracking. The case study asks for a progression system,
/// not a meta-game; those extras are easy to layer on top of this service
/// without reshaping its API.
///
/// Null-safe against a missing catalog so the GameplayLifetimeScope can
/// still register the service when no catalog is assigned (in which case
/// the bootstrapper falls back to a hand-picked starting level).
/// </summary>
public sealed class LevelProgressionService
{
    private const string PrefKey = "blockflow.current_level_index";

    private readonly LevelCatalog catalog;

    public LevelCatalog Catalog => catalog;
    public int CurrentIndex { get; private set; }

    public LevelConfig Current => catalog != null ? catalog.GetAt(CurrentIndex) : null;
    public LevelConfig Next    => catalog != null ? catalog.GetAt(CurrentIndex + 1) : null;
    public bool HasNext        => Next != null;

    public LevelProgressionService(LevelCatalog catalog)
    {
        this.catalog = catalog;

        if (catalog == null)
        {
            CurrentIndex = 0;
            return;
        }

        int saved = PlayerPrefs.GetInt(PrefKey, 0);
        CurrentIndex = Mathf.Clamp(saved, 0, Mathf.Max(0, catalog.Count - 1));
    }

    /// <summary>
    /// Advances the current index if a next level exists and persists the
    /// new value. Returns true if the index actually moved.
    /// </summary>
    public bool AdvanceToNext()
    {
        if (!HasNext) return false;
        CurrentIndex++;
        Save();
        return true;
    }

    /// <summary>
    /// Jumps directly to a specific catalog index. Useful for debug / level
    /// select. Clamps to the catalog's valid range.
    /// </summary>
    public void SetIndex(int index)
    {
        if (catalog == null || catalog.Count == 0) return;
        CurrentIndex = Mathf.Clamp(index, 0, catalog.Count - 1);
        Save();
    }

    /// <summary>
    /// Resets progression to the first level and persists. Handy for
    /// hitting "New Game" in a future main menu.
    /// </summary>
    public void Reset()
    {
        CurrentIndex = 0;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(PrefKey, CurrentIndex);
        PlayerPrefs.Save();
    }
}
