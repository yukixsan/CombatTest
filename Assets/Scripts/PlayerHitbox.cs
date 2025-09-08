using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public Transform Owner { get; private set; }
    public HitboxPayload Payload { get; private set; }
    public bool HasPayload { get; private set; }

    [SerializeField] private Collider _collider; // assign in inspector

    public void Initialize(Transform owner)
    {
        Owner = owner;
        HasPayload = false;
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
