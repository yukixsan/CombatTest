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
        if (stateController != null && healthComponent.CanBeInterruptedBy(payload.AttackerArmor))
        {
           stateController.TriggerDamaged(payload);
        }
        // Knockback — direction relative to attacker position
        float facingX = Mathf.Sign(transform.position.x - payload.attacker.position.x);
        Vector3 knockback = new Vector3(
            payload.KnockbackForce * facingX,
            payload.LaunchForce * payload.LaunchDir,
            0f
        );
        //rb.AddForce(knockback, ForceMode.Impulse);
        // if (enemyAI != null)
        // {
        //     //enemyAI.ApplyKnocback(moveLockDuration);
        //     enemyAI.ApplyKnockback(knockback, moveLockDuration);
        // }

        if (stateController != null)
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
