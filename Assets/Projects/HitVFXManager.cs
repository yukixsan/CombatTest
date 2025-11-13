using UnityEngine;
using System.Collections.Generic;


public class HitVFXManager : MonoBehaviour
{
    public static HitVFXManager Instance { get; private set; }

    [SerializeField] private GameObject[] vfxPrefabs;
    private List<Queue<GameObject>> _pools = new();

    private void Awake()
    {
        Instance = this;
        // Initialize pool per prefab
        foreach (var prefab in vfxPrefabs)
        {
            var q = new Queue<GameObject>();
            _pools.Add(q);
        }
    }

    public GameObject SpawnVFX(int index, Vector3 position, Quaternion rotation)
    {
        if (index < 0 || index >= vfxPrefabs.Length)
            return null;

        GameObject obj;
        if (_pools[index].Count > 0)
        {
            obj = _pools[index].Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(vfxPrefabs[index]);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    public void DespawnVFX(int index, GameObject obj)
    {
        obj.SetActive(false);
        _pools[index].Enqueue(obj);
    }
}
