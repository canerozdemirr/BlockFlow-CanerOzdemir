using UnityEngine;

public interface ISaveRepository
{
    int GetInt(string key, int defaultValue = 0);
    void SetInt(string key, int value);
    void Save();
}

// PlayerPrefs.Save is called on every Save() because mobile OS kills don't flush otherwise.
public sealed class PlayerPrefsSaveRepository : ISaveRepository
{
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void Save() => PlayerPrefs.Save();
}
