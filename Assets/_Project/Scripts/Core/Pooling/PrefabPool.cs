using System;
using UnityEngine;
using UnityEngine.Pool;

// One pool instance per prefab. See BlockViewFactory / GrinderViewFactory for multi-prefab usage.
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

    public void Clear() => pool.Clear();

    private T Create()
    {
        T instance = UnityEngine.Object.Instantiate(prefab, parent);
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
