using UnityEngine;

/// <summary>
/// Minimal abstraction for loading runtime prefabs by name. Default resolution
/// hits the Resources folder with an editor-only AssetDatabase fallback; tests
/// swap this for a fake that returns hand-crafted GameObjects.
/// </summary>
public interface IPrefabLoader
{
    GameObject Load(string resourceName);
}

/// <summary>
/// Default prefab loader — caches results per name so repeated calls don't
/// hit Resources/AssetDatabase twice.
/// </summary>
public sealed class ResourcesPrefabLoader : IPrefabLoader
{
    private readonly System.Collections.Generic.Dictionary<string, GameObject> cache =
        new System.Collections.Generic.Dictionary<string, GameObject>();

    public GameObject Load(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        if (cache.TryGetValue(resourceName, out var cached) && cached != null) return cached;

        var go = Resources.Load<GameObject>(resourceName);
#if UNITY_EDITOR
        if (go == null)
            go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Project/Prefabs/Gameplay/{resourceName}.prefab");
#endif
        if (go != null) cache[resourceName] = go;
        return go;
    }
}
