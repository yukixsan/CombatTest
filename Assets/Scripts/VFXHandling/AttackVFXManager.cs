using UnityEngine;
using System.Collections.Generic;

public class AttackVFXManager : MonoBehaviour
{
    public static AttackVFXManager Instance { get; private set; }

    // Pool keyed by prefab reference
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    // Track active so we can StopAll on cancel
    private readonly List<(GameObject prefab, GameObject instance)> _active = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

   public GameObject Play(AttackPhaseVFX phaseVFX, Transform attachTo)
    {
        if (phaseVFX.prefab == null) return null;

        var obj = GetFromPool(phaseVFX.prefab);

        if (attachTo != null)
        {
            obj.transform.SetParent(attachTo);
            obj.transform.localPosition = phaseVFX.localOffset;
            obj.transform.localRotation = Quaternion.identity;
        }

        obj.SetActive(true);

        var ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // wipe any leftover state
            ps.Play(true); // guaranteed clean start
        }

        var poolReturn = obj.GetComponent<VFXPoolReturn>();
        if (poolReturn != null) poolReturn.SetPrefabKey(phaseVFX.prefab);

        _active.Add((phaseVFX.prefab, obj));
        return obj;
    }

    // Called on cancel/reset only — normal phase transitions let particles expire naturally
    public void StopAll()
{
    foreach (var (prefab, obj) in _active)
    {
        if (obj == null) continue;

        var ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        obj.transform.SetParent(null);
        obj.SetActive(false);

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        _pools[prefab].Enqueue(obj);
    }
    _active.Clear();
}

    public void ReturnToPool(GameObject prefab, GameObject instance)
    {
        instance.transform.SetParent(null);
        instance.SetActive(false);

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        _pools[prefab].Enqueue(instance);
        _active.RemoveAll(e => e.instance == instance);
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
            return queue.Dequeue();

        return Instantiate(prefab);
    }
}