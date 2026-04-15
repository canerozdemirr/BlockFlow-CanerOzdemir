using UnityEngine;

// Default resolution hits Resources; editor-only AssetDatabase fallback covers prefabs
// that designers haven't moved under Resources/ yet.
public interface IPrefabLoader
{
    GameObject Load(string resourceName);
}

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
