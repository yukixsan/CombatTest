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

        Collider[] overlaps = Physics.OverlapBox(
            _collider.bounds.center,
            _collider.bounds.extents,
            _collider.transform.rotation,
            _enemyLayer,
            QueryTriggerInteraction.Collide  // must have this
        );

        foreach (var col in overlaps)
        {
            var hurtbox = col.GetComponent<EnemyHurtbox>();
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
         // ✅ Immediately check overlaps so hits don't get missed
        Collider[] overlaps = Physics.OverlapBox(
            _collider.bounds.center,
            _collider.bounds.extents,
            _collider.transform.rotation
        );

        foreach (var col in overlaps)
        {
            var hurtbox = col.GetComponent<EnemyHurtbox>();
            if (hurtbox != null)
            {
                hurtbox.TryTakeHit(this);
            }
        }
    }

    public void ClearPayload()
    {
        HasPayload = false;
    }
   
}
