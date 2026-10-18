using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurstMark : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerHitbox _hitbox;
    [SerializeField] private GameObject _idleVisual;

    private HitboxPayload _payload;
    private float _lifetime;
    private float _timer;
    private bool _isDetonated;
    private GameObject _prefabKey;

    private GameObject _detonationVFXPrefab;
    private AudioClip _detonationSFX;
    private float _detonationSFXVolume = 1f;

    // Static registry for Phase 4 forced detonation
    private static readonly List<BurstMark> _activeMarks = new();
    public static IReadOnlyList<BurstMark> ActiveMarks => _activeMarks;

    public static void DetonateAll()
    {
        var marks = new List<BurstMark>(_activeMarks);
        foreach (var mark in marks)
        {
            if (mark != null && !mark._isDetonated)
            {
                mark.Detonate();
            }
        }
    }

    public static void DetonateSequentially(MonoBehaviour runner, float interval = 0.08f)
    {
        if (runner == null)
        {
            DetonateAll();
            return;
        }

        runner.StartCoroutine(DetonateSequentiallyRoutine(interval));
    }

    private static IEnumerator DetonateSequentiallyRoutine(float interval)
    {
        // Snapshot current marks in chronological order (FIFO: index 0 is oldest spawned mark)
        var marks = new List<BurstMark>(_activeMarks);
        for (int i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (mark != null && !mark._isDetonated)
            {
                mark.Detonate();

                if (i < marks.Count - 1 && interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }
    }

    public void Initialize(HitboxPayload payload, float lifetime, GameObject prefabKey, BurstDashData data, Transform attacker)
    {
        _payload = payload;
        _lifetime = lifetime;
        _prefabKey = prefabKey;
        _timer = 0f;
        _isDetonated = false;

        if (data != null)
        {
            _detonationVFXPrefab = data.detonationVFXPrefab;
            _detonationSFX = data.detonationSFX;
            _detonationSFXVolume = data.detonationSFXVolume;

            // Spawn VFX
            if (data.spawnVFXPrefab != null)
            {
                Instantiate(data.spawnVFXPrefab, transform.position, Quaternion.identity);
            }

            // Play spawn SFX
            if (data.spawnSFX != null && SFXManager.Instance != null)
            {
                SFXManager.Instance.PlaySFX(data.spawnSFX, data.spawnSFXVolume);
            }
        }

        if (_idleVisual != null)
            _idleVisual.SetActive(true);

        if (_hitbox != null)
        {
            _hitbox.Initialize(attacker);
            _hitbox.DeactivateHitbox();
        }

        if (!_activeMarks.Contains(this))
            _activeMarks.Add(this);

        Debug.Log($"[BurstMark] Spawned at {transform.position}. Lifetime: {_lifetime}s, Dmg: {_payload.Damage}, LaunchDir: {_payload.LaunchDir}, Knockback: {_payload.KnockbackForce}");
    }

    private void Update()
    {
        if (_isDetonated) return;

        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            Detonate();
        }
    }

    public void Detonate()
    {
        if (_isDetonated) return;
        _isDetonated = true;
        _activeMarks.Remove(this);

        Debug.Log($"[BurstMark] Detonating at {transform.position}");

        if (_idleVisual != null)
            _idleVisual.SetActive(false);

        // Spawn detonation VFX
        if (_detonationVFXPrefab != null)
        {
            Instantiate(_detonationVFXPrefab, transform.position, Quaternion.identity);
        }

        // Play detonation SFX
        if (_detonationSFX != null && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(_detonationSFX, _detonationSFXVolume);
        }

        // Activate hitbox (immediate check + fixed update polling)
        if (_hitbox != null)
        {
            _hitbox.SetPayload(_payload);
        }

        StartCoroutine(CleanupRoutine());
    }

    private IEnumerator CleanupRoutine()
    {
        // Allow hitbox to remain active for 2 physics frames to catch overlaps
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (_hitbox != null)
        {
            _hitbox.DeactivateHitbox();
        }

        if (_prefabKey != null && SkillObjectPool.Instance != null)
        {
            SkillObjectPool.Instance.Return(_prefabKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        _activeMarks.Remove(this);
    }
}

