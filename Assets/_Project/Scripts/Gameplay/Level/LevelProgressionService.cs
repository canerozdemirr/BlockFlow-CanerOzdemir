using UnityEngine;

/// <summary>
/// Tracks the player's continuous level number and persists it to
/// <see cref="PlayerPrefs"/>. The level catalog loops endlessly —
/// after the last level, the player goes back to the first catalog
/// entry but the level number keeps incrementing.
///
/// <see cref="CurrentIndex"/> is the continuous level number (0-based).
/// <see cref="CatalogIndex"/> is the modulo index into the catalog.
/// <see cref="Current"/> returns the <see cref="LevelConfig"/> at the
/// catalog index.
/// </summary>
public sealed class LevelProgressionService
{
    private const string PrefKey = "blockflow.current_level_index";

    private readonly LevelCatalog catalog;

    public LevelCatalog Catalog => catalog;

    /// <summary>Continuous level index (0-based). Keeps incrementing past catalog size.</summary>
    public int CurrentIndex { get; private set; }

    /// <summary>The display level number (1-based).</summary>
    public int LevelNumber => CurrentIndex + 1;

    /// <summary>Index into the catalog (loops via modulo).</summary>
    public int CatalogIndex => catalog != null && catalog.Count > 0
        ? CurrentIndex % catalog.Count : 0;

    /// <summary>The current level config (loops the catalog).</summary>
    public LevelConfig Current => catalog != null ? catalog.GetAt(CatalogIndex) : null;

    public LevelProgressionService(LevelCatalog catalog)
    {
        this.catalog = catalog;

        if (catalog == null)
        {
            CurrentIndex = 0;
            return;
        }

        CurrentIndex = PlayerPrefs.GetInt(PrefKey, 0);
        if (CurrentIndex < 0) CurrentIndex = 0;
    }

    /// <summary>
    /// Re-reads the current index from PlayerPrefs. Call this when returning
    /// from another scene that may have advanced the progression.
    /// </summary>
    public void ReloadFromDisk()
    {
        CurrentIndex = PlayerPrefs.GetInt(PrefKey, 0);
        if (CurrentIndex < 0) CurrentIndex = 0;
    }

    /// <summary>
    /// Advances to the next level. Always succeeds — the catalog loops.
    /// </summary>
    public void AdvanceToNext()
    {
        CurrentIndex++;
        Save();
    }

    /// <summary>
    /// Jumps directly to a specific continuous index.
    /// </summary>
    public void SetIndex(int index)
    {
        CurrentIndex = Mathf.Max(0, index);
        Save();
    }

    /// <summary>
    /// Resets progression to level 0.
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
