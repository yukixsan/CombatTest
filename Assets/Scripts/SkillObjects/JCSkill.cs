using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JCSkill : SkillObject
{
    [Header("Scan area")]
    [SerializeField] private Vector2 scanBoxSize = new Vector2(8f, 6f);
    [SerializeField] private Vector2 scanBoxOffset = Vector2.zero;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxScanLifetime = 0.2f;

    [Header("Multi-Target Settings")]
    [Tooltip("Maximum number of distinct enemies to strike.")]
    [SerializeField] private int maxTargets = 3;
    [Tooltip("Delay in seconds between successive slashes. 0 = strike all detected targets simultaneously.")]
    [SerializeField] private float delayBetweenHits = 0.05f;

    [Header("Hit object")]
    [SerializeField] private GameObject jcHitPrefab;

    private readonly HashSet<EnemyHurtbox> _targetedHurtboxes = new();
    private float _scanTimer = 0f;
    private int _targetsHitCount = 0;
    private bool _isSpawningSequential = false;

    public override void Initialize(PlayerSkillData data, Transform player)
    {
        base.Initialize(data, player);
        _scanTimer = maxScanLifetime;
        _targetsHitCount = 0;
        _isSpawningSequential = false;
        _targetedHurtboxes.Clear();

        // Calculate total pool lifetime to accommodate sequential hit delays
        float totalLifetime = maxScanLifetime + (maxTargets * Mathf.Max(0f, delayBetweenHits)) + 0.3f;
        var returnComp = GetComponent<SkillObjectReturn>();
        if (returnComp != null)
            returnComp.Setup(data.skillPrefab, totalLifetime);
    }

    private void Update()
    {
        if (_targetsHitCount >= maxTargets || _isSpawningSequential) return;

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f) return;

        ScanArea();
    }

    private void ScanArea()
    {
        Vector3 centerWorld = transform.position + new Vector3(scanBoxOffset.x * _facing, scanBoxOffset.y, 0f);
        Collider[] hits = Physics.OverlapBox(
            centerWorld,
            new Vector3(scanBoxSize.x * 0.5f, scanBoxSize.y * 0.5f, 0.5f),
            Quaternion.identity,
            enemyLayer
        );

        if (hits.Length <= 0) return;

        // Group by EnemyHurtbox to prevent multi-collider body parts from registering as separate targets
        var candidates = new List<(EnemyHurtbox hurtbox, Vector3 position, float y, float dist)>();
        var seenThisScan = new HashSet<EnemyHurtbox>();

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            var hurtbox = col.GetComponent<EnemyHurtbox>()
                       ?? col.GetComponentInParent<EnemyHurtbox>()
                       ?? col.GetComponentInChildren<EnemyHurtbox>();

            if (hurtbox == null || _targetedHurtboxes.Contains(hurtbox) || seenThisScan.Contains(hurtbox))
                continue;

            seenThisScan.Add(hurtbox);

            Vector3 enemyPos = hurtbox.transform.position;
            float dist = Vector3.Distance(transform.position, enemyPos);
            candidates.Add((hurtbox, enemyPos, enemyPos.y, dist));
        }

        if (candidates.Count == 0) return;

        // Sort candidates: lowest Y (grounded) first, then closest distance
        candidates.Sort((a, b) =>
        {
            if (!Mathf.Approximately(a.y, b.y))
                return a.y.CompareTo(b.y);
            return a.dist.CompareTo(b.dist);
        });

        int remainingQuota = maxTargets - _targetsHitCount;
        int countToTake = Mathf.Min(remainingQuota, candidates.Count);

        var hitPositions = new List<Vector3>(countToTake);
        for (int i = 0; i < countToTake; i++)
        {
            var candidate = candidates[i];
            _targetedHurtboxes.Add(candidate.hurtbox);
            _targetsHitCount++;
            hitPositions.Add(candidate.position);
        }

        if (delayBetweenHits <= 0f)
        {
            for (int i = 0; i < hitPositions.Count; i++)
            {
                SpawnJCHit(hitPositions[i]);
            }
        }
        else
        {
            StartCoroutine(SpawnSequentialHitsRoutine(hitPositions));
        }
    }

    private IEnumerator SpawnSequentialHitsRoutine(List<Vector3> positions)
    {
        _isSpawningSequential = true;

        for (int i = 0; i < positions.Count; i++)
        {
            SpawnJCHit(positions[i]);

            if (i < positions.Count - 1 && delayBetweenHits > 0f)
            {
                yield return new WaitForSeconds(delayBetweenHits);
            }
        }

        _isSpawningSequential = false;
    }

    private void SpawnJCHit(Vector3 hitPosition)
    {
        if (jcHitPrefab == null) return;

        GameObject jcHitObj = Instantiate(jcHitPrefab, hitPosition, Quaternion.identity);
        var hit = jcHitObj.GetComponent<JCHitObject>();
        if (hit != null)
        {
            hit.Initialize(payload, _facing);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isSpawningSequential = false;
        _targetedHurtboxes.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(scanBoxOffset.x * _facing, scanBoxOffset.y, 0f);
        Gizmos.DrawWireCube(center, new Vector3(scanBoxSize.x, scanBoxSize.y, 1f));
    }
}
