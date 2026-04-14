using UnityEngine;
using System.Collections.Generic;

public class SkillObjectPool : MonoBehaviour
{
    public static SkillObjectPool Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var obj = GetFromPool(prefab);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(null);

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        _pools[prefab].Enqueue(instance);
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
            return queue.Dequeue();

        return Instantiate(prefab);
    }
}