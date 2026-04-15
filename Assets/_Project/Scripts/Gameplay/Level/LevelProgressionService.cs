using UnityEngine;

// Catalog loops endlessly: past the last entry the player wraps to the first
// catalog level but CurrentIndex / LevelNumber keep incrementing.
public sealed class LevelProgressionService
{
    private const string PrefKey = "blockflow.current_level_index";

    private readonly LevelCatalog catalog;
    private readonly ISaveRepository save;

    public LevelCatalog Catalog => catalog;

    public int CurrentIndex { get; private set; }

    // 1-based display number.
    public int LevelNumber => CurrentIndex + 1;

    public int CatalogIndex => catalog != null && catalog.Count > 0
        ? CurrentIndex % catalog.Count : 0;

    public LevelConfig Current => catalog != null ? catalog.GetAt(CatalogIndex) : null;

    public LevelProgressionService(LevelCatalog catalog, ISaveRepository save)
    {
        this.catalog = catalog;
        this.save = save ?? new PlayerPrefsSaveRepository();

        if (catalog == null)
        {
            CurrentIndex = 0;
            return;
        }

        CurrentIndex = this.save.GetInt(PrefKey, 0);
        if (CurrentIndex < 0) CurrentIndex = 0;
    }

    // Call when returning from a scene that may have advanced progression.
    public void ReloadFromDisk()
    {
        CurrentIndex = save.GetInt(PrefKey, 0);
        if (CurrentIndex < 0) CurrentIndex = 0;
    }

    public void AdvanceToNext()
    {
        CurrentIndex++;
        Save();
    }

    public void SetIndex(int index)
    {
        CurrentIndex = Mathf.Max(0, index);
        Save();
    }

    public void Reset()
    {
        CurrentIndex = 0;
        Save();
    }

    private void Save()
    {
        save.SetInt(PrefKey, CurrentIndex);
        save.Save();
    }
}
