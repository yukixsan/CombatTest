using UnityEngine;
using System.Collections.Generic;

public class HitVFXManager : MonoBehaviour
{
    public static HitVFXManager Instance { get; private set; }

    [SerializeField] private GameObject[] vfxPrefabs;
    [SerializeField] private GameObject[] awakenedVfxPrefabs;

    private List<Queue<GameObject>> _pools = new();
    private List<Queue<GameObject>> _awakenedPools = new();

    private void Awake()
    {
        Instance = this;
        // Initialize pool per base prefab
        if (vfxPrefabs != null)
        {
            for (int i = 0; i < vfxPrefabs.Length; i++)
            {
                _pools.Add(new Queue<GameObject>());
            }
        }

        // Initialize pool per awakened prefab
        if (awakenedVfxPrefabs != null)
        {
            for (int i = 0; i < awakenedVfxPrefabs.Length; i++)
            {
                _awakenedPools.Add(new Queue<GameObject>());
            }
        }
    }

    public GameObject SpawnVFX(int index, Vector3 position, Quaternion rotation)
    {
        if (index < 0 || vfxPrefabs == null || index >= vfxPrefabs.Length)
            return null;

        bool isAwakened = AwakeningManager.Instance != null && AwakeningManager.Instance.IsAwakened;
        bool hasAwakenedOverride = isAwakened
            && awakenedVfxPrefabs != null
            && index < awakenedVfxPrefabs.Length
            && awakenedVfxPrefabs[index] != null
            && index < _awakenedPools.Count;

        GameObject obj = hasAwakenedOverride
            ? GetOrCreateInstance(_awakenedPools[index], awakenedVfxPrefabs[index])
            : GetOrCreateInstance(_pools[index], vfxPrefabs[index]);

        if (obj != null)
        {
            obj.transform.SetPositionAndRotation(position, rotation);
        }

        return obj;
    }

    private GameObject GetOrCreateInstance(Queue<GameObject> pool, GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab);
        }

        return obj;
    }

    public void DespawnVFX(int index, GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);

        if (awakenedVfxPrefabs != null && index >= 0 && index < _awakenedPools.Count &&
            awakenedVfxPrefabs[index] != null && obj.name.StartsWith(awakenedVfxPrefabs[index].name))
        {
            _awakenedPools[index].Enqueue(obj);
        }
        else if (index >= 0 && index < _pools.Count)
        {
            _pools[index].Enqueue(obj);
        }
    }
}
