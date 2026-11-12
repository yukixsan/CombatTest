using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BurstSkill : SkillObject
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [FormerlySerializedAs("rb3d")]
    [SerializeField] private Rigidbody rb;

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

        // Ensure 3D Rigidbody exists
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
            }
        }

        // transform.right is already aligned with street forward, player facing, and downward tilt by SkillObject.Initialize
        Vector3 dir = transform.right;
        _travelVelocity = dir * speed;

        var mover = player.GetComponentInParent<PlayerMovement>();
        bool isZStreet = mover != null ? (Mathf.Abs(mover.StreetForward.z) > 0.5f) : (Mathf.Abs(transform.forward.x) > 0.5f);
        if (rb != null)
        {
            RigidbodyConstraints posFreeze = isZStreet ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionZ;
            rb.constraints = posFreeze | RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = _travelVelocity;
        }

        // Pool auto-return
        var returnComp = GetComponent<SkillObjectReturn>();
        if (returnComp != null)
            returnComp.Setup(data.skillPrefab, lifetime);
    }

    private void FixedUpdate()
    {
        // Maintain projectile movement
        if (rb != null) rb.linearVelocity = _travelVelocity;

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
        bool isZStreet = Mathf.Abs(_travelVelocity.z) > Mathf.Abs(_travelVelocity.x);

        for (int i = _draggedEnemies.Count - 1; i >= 0; i--)
        {
            var enemyRb = _draggedEnemies[i];
            if (enemyRb == null || !enemyRb.gameObject.activeInHierarchy)
            {
                _draggedEnemies.RemoveAt(i);
                continue;
            }

            // Keep depth axis frozen to 2.5D plane
            Vector3 toCenter = transform.position - enemyRb.position;
            if (isZStreet)
                toCenter.x = 0f;
            else
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
