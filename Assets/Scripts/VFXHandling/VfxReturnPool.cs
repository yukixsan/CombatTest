using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VFXPoolReturn : MonoBehaviour
{
    private ParticleSystem _ps;
    private GameObject _prefabKey; // set at spawn time

    private void Awake() => _ps = GetComponent<ParticleSystem>();

    public void SetPrefabKey(GameObject key) => _prefabKey = key;

    private void Update()
    {
        if (_prefabKey == null) return;
        if(!gameObject.activeSelf) return;
        if (!_ps.IsAlive(true))
            AttackVFXManager.Instance.ReturnToPool(_prefabKey, gameObject);
    }
}
