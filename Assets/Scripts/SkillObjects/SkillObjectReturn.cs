using UnityEngine;

/// <summary>
/// Add to any SkillObject prefab that needs lifetime-based pooling.
/// Call Setup() from the SkillObject's Initialize override.
/// </summary>
public class SkillObjectReturn : MonoBehaviour
{
    private GameObject _prefabKey;
    private float _returnTime;
    private bool _armed;

    public void Setup(GameObject prefabKey, float lifetime)
    {
        _prefabKey = prefabKey;
        _returnTime = Time.time + lifetime;
        _armed = true;
    }

    private void OnEnable()
    {
        // Reset arm state so a re-pooled object doesn't immediately return
        _armed = false;
    }

    private void Update()
    {
        if (!_armed || _prefabKey == null) return;

        if (Time.time >= _returnTime)
        {
            _armed = false;
            SkillObjectPool.Instance.Return(_prefabKey, gameObject);
        }
    }
}