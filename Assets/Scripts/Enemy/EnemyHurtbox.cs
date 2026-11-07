using Unity.VisualScripting;
using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
   
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private float moveLockDuration = 0.5f;

 
    public void TryTakeHit(PlayerHitbox hitbox)
    {
        if (!hitbox.HasPayload) return;
 
        HitboxPayload payload = hitbox.Payload;
        var stateController = GetComponentInParent<EnemyStateController>();

        // Interrupt enemy attack if the payload's armor value is sufficient
        bool interrupted = healthComponent != null && healthComponent.CanBeInterruptedBy(payload.AttackerArmor);
        if (stateController != null && interrupted)
        {
            stateController.TriggerDamaged(payload);
        }

        // Hit VFX at enemy hurtbox center
        Collider collider = GetComponent<Collider>();
        Vector3 hitPoint = collider != null ? collider.bounds.center : transform.position;
        HitVFXManager.Instance.SpawnVFX(payload.VFXindex, hitPoint, Quaternion.identity);
 
        // Hitstop
        HitStopManager.Instance.StartHitstop(payload.HitstopDuration);

        //Combo count
        ComboCountManager.Instance.RegisterHit();
 
        // Health — all downstream events (damage, stun, die) flow through HealthComponent → EnemyHealth
        healthComponent.TakeDamage(payload.Damage, 20);
    }

    
}
