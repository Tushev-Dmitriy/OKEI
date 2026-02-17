using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool<T> where T : MonoBehaviour
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Stack<T> pool = new Stack<T>();

    public SimpleObjectPool(T prefab, Transform parent, int prewarmCount)
    {
        this.prefab = prefab;
        this.parent = parent;

        if (prewarmCount > 0)
        {
            Prewarm(prewarmCount);
        }
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            AddNewInstance();
        }
    }

    public T Get()
    {
        if (pool.Count == 0)
        {
            AddNewInstance();
        }

        T instance = pool.Pop();
        instance.gameObject.SetActive(true);
        return instance;
    }

    public void Release(T instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.gameObject.SetActive(false);
        instance.transform.SetParent(parent, false);
        pool.Push(instance);
    }

    private void AddNewInstance()
    {
        T instance = Object.Instantiate(prefab, parent);
        instance.gameObject.SetActive(false);
        pool.Push(instance);
    }
}
