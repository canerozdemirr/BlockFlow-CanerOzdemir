using UnityEngine;

/// <summary>
/// Minimal key/value persistence boundary. Keeps storage decisions (PlayerPrefs
/// now, cloud save / JSON file later) out of services like
/// <see cref="LevelProgressionService"/>, which only know the keys they own.
/// </summary>
public interface ISaveRepository
{
    int GetInt(string key, int defaultValue = 0);
    void SetInt(string key, int value);
    void Save();
}

/// <summary>
/// Default PlayerPrefs-backed implementation. Calls <c>PlayerPrefs.Save</c>
/// on every <see cref="Save"/> because mobile OS kills don't flush otherwise.
/// </summary>
public sealed class PlayerPrefsSaveRepository : ISaveRepository
{
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void Save() => PlayerPrefs.Save();
}
