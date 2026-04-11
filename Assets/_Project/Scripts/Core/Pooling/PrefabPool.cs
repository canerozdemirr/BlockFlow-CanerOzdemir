using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Thin wrapper around <see cref="ObjectPool{T}"/> for Component-rooted prefabs.
/// Handles instantiation, parenting and activate/deactivate on get/release so
/// call sites can treat pooled objects the same way they would a freshly
/// instantiated prefab.
///
/// A single pool instance is bound to exactly one prefab. Projects that need
/// multiple prefabs should keep a dictionary of <see cref="PrefabPool{T}"/>
/// keyed by prefab reference (see BlockViewFactory / GrinderViewFactory).
/// </summary>
public sealed class PrefabPool<T> where T : Component
{
    private readonly ObjectPool<T> pool;
    private readonly T prefab;
    private readonly Transform parent;

    public PrefabPool(T prefab, Transform parent, int defaultCapacity = 16, int maxSize = 256)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        this.prefab = prefab;
        this.parent = parent;

        pool = new ObjectPool<T>(
            createFunc:      Create,
            actionOnGet:     OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyInstance,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize);
    }

    public T Get() => pool.Get();

    public void Release(T instance)
    {
        if (instance != null) pool.Release(instance);
    }

    /// <summary>
    /// Destroys every pooled instance and resets the pool. Call between
    /// levels to reclaim memory; new instances will be freshly instantiated
    /// on the next <see cref="Get"/>.
    /// </summary>
    public void Clear() => pool.Clear();

    // ---------- callbacks ----------

    private T Create()
    {
        var instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private static void OnGet(T instance)
    {
        instance.gameObject.SetActive(true);
    }

    private static void OnRelease(T instance)
    {
        instance.gameObject.SetActive(false);
    }

    private static void OnDestroyInstance(T instance)
    {
        if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
    }
}
