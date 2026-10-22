using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BurstSkill : SkillObject
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private Rigidbody rb3d;
    [FormerlySerializedAs("rb")]
    [SerializeField] private Rigidbody2D rb2d;

    [Header("Drag Settings")]
    [Tooltip("Radius around the projectile center within which enemies are caught and dragged.")]
    [SerializeField] private float dragRadius = 2f;
    [Tooltip("Layer mask for enemies (defaults to Enemy layer 6).")]
    [SerializeField] private LayerMask enemyLayer = 1 << 6;
    [Tooltip("Strength with which caught enemies are pulled toward the projectile center.")]
    [SerializeField] private float centeringStrength = 8f;
    [Tooltip("Multiplier applied to projectile velocity on release.")]
    [SerializeField] private float releaseMomentumMultiplier = 0.8f;

    [Header("Multi-Hit Settings")]
    [Tooltip("Whether this projectile strikes repeatedly at fixed intervals.")]
    [SerializeField] private bool canMultiHit = false;
    [Tooltip("Time interval in seconds between consecutive hits.")]
    [SerializeField] private float hitInterval = 0.1f;

    private readonly List<Rigidbody> _draggedEnemies = new();
    private Vector3 _travelVelocity;
    private float _hitTimer;

    public override void Initialize(PlayerSkillData data, Transform player)
    {
        base.Initialize(data, player);

        _hitTimer = 0f;
        if (_hitbox == null) _hitbox = GetComponentInChildren<PlayerHitbox>();

        if (rb3d == null) rb3d = GetComponent<Rigidbody>();
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();

        // Calculate travel direction along local x-axis, flipped by player facing
        Vector3 dir = transform.right * _facing;
        _travelVelocity = dir * speed;

        if (rb3d != null) rb3d.linearVelocity = _travelVelocity;
        if (rb2d != null) rb2d.linearVelocity = _travelVelocity;

        // Pool auto-return
        var returnComp = GetComponent<SkillObjectReturn>();
        if (returnComp != null)
            returnComp.Setup(data.skillPrefab, lifetime);
    }

    private void FixedUpdate()
    {
        // Maintain projectile movement
        if (rb3d != null) rb3d.linearVelocity = _travelVelocity;
        if (rb2d != null) rb2d.linearVelocity = _travelVelocity;

        CaptureEnemies();
        DragEnemies();

        // Multi-hit interval tick
        if (canMultiHit && _hitbox != null)
        {
            _hitTimer += Time.fixedDeltaTime;
            if (_hitTimer >= hitInterval)
            {
                _hitTimer = 0f;
                _hitbox.ActivateHitbox(payload);
            }
        }
    }

    private void CaptureEnemies()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, dragRadius, enemyLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlaps.Length; i++)
        {
            var col = overlaps[i];
            var enemyRb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (enemyRb != null && !_draggedEnemies.Contains(enemyRb))
            {
                // Disengage gravity while dragged so enemies don't fall out of airborne trajectory
                enemyRb.useGravity = false;
                _draggedEnemies.Add(enemyRb);
            }
        }
    }

    private void DragEnemies()
    {
        for (int i = _draggedEnemies.Count - 1; i >= 0; i--)
        {
            var enemyRb = _draggedEnemies[i];
            if (enemyRb == null || !enemyRb.gameObject.activeInHierarchy)
            {
                _draggedEnemies.RemoveAt(i);
                continue;
            }

            // Keep Z frozen to 2.5D plane
            Vector3 toCenter = transform.position - enemyRb.position;
            toCenter.z = 0f;

            // Velocity drag: match projectile velocity + soft pull toward projectile center
            enemyRb.linearVelocity = _travelVelocity + (toCenter * centeringStrength);
        }
    }

    private void ReleaseEnemies()
    {
        for (int i = 0; i < _draggedEnemies.Count; i++)
        {
            var enemyRb = _draggedEnemies[i];
            if (enemyRb != null)
            {
                enemyRb.useGravity = true;
                enemyRb.linearVelocity = _travelVelocity * releaseMomentumMultiplier;
            }
        }
        _draggedEnemies.Clear();
    }

    private void OnDisable()
    {
        _hitTimer = 0f;
        ReleaseEnemies();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ReleaseEnemies();
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ReleaseEnemies();
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dragRadius);
    }
}
