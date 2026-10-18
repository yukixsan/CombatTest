using System.Collections.Generic;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public Transform Owner { get; private set; }
    public HitboxPayload Payload { get; private set; }
    public bool HasPayload { get; private set; }

    [SerializeField] private Collider _collider; // assign in inspector
    [SerializeField] private LayerMask _enemyLayer; // assign in inspector
    private readonly HashSet<EnemyHurtbox> _alreadyHit = new();
    
    public void Initialize(Transform owner)
    {
        Owner = owner;
        HasPayload = false;
        _collider.enabled = false; // ensure collider is active
        _alreadyHit.Clear();

    }
    private void FixedUpdate()
    {
        if (!HasPayload) return;

        CheckOverlaps();
    }

    private void CheckOverlaps()
    {
        if (_collider == null) return;

        Vector3 center;
        Vector3 halfExtents;
        Quaternion rotation;

        if (_collider is BoxCollider box)
        {
            center = box.transform.TransformPoint(box.center);
            Vector3 lossy = box.transform.lossyScale;
            halfExtents = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
            rotation = box.transform.rotation;
        }
        else
        {
            center = _collider.bounds.center;
            halfExtents = _collider.bounds.extents;
            rotation = _collider.transform.rotation;
        }

        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            _enemyLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (var col in overlaps)
        {
            var hurtbox = col.GetComponent<EnemyHurtbox>()
                       ?? col.GetComponentInParent<EnemyHurtbox>()
                       ?? col.GetComponentInChildren<EnemyHurtbox>();

            if (hurtbox == null || _alreadyHit.Contains(hurtbox)) continue;

            _alreadyHit.Add(hurtbox);
            hurtbox.TryTakeHit(this);
        }
    }

    public void ActivateHitbox(HitboxPayload hitbox )
    {
       Payload = hitbox;
       HasPayload = true;
       _alreadyHit.Clear(); // Clear hit tracking for new activation
    }

    public void DeactivateHitbox()
    {
        HasPayload = false; // Clear payload when deactivating hitbox
        _alreadyHit.Clear(); // Clear hit tracking when deactivating
    }

    public void SetPayload(HitboxPayload payload)
    {
        Payload = payload;
        HasPayload = true;
        // Immediately check overlaps so hits don't get missed
        CheckOverlaps();
    }

    public void ClearPayload()
    {
        HasPayload = false;
    }
   
}
