using UnityEngine;

public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private PlayerStateController playerStateController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            // 1. Authoritative: Find the attacking enemy's EnemyStateController and read its active hitbox
            EnemyHitBox enemyHitbox = other.GetComponentInParent<EnemyStateController>()?.Hitbox;

            // 2. If not found or controller's hitbox has no payload, find any EnemyHitBox in parents that HAS a payload
            if (enemyHitbox == null || !enemyHitbox.HasPayload)
            {
                var hitboxes = other.GetComponentsInParent<EnemyHitBox>();
                foreach (var hb in hitboxes)
                {
                    if (hb != null && hb.HasPayload)
                    {
                        enemyHitbox = hb;
                        break;
                    }
                }
            }

            // 3. Fallback to direct component or attached Rigidbody lookup
            if (enemyHitbox == null)
            {
                enemyHitbox = other.GetComponent<EnemyHitBox>()
                           ?? other.GetComponentInParent<EnemyHitBox>()
                           ?? other.GetComponentInChildren<EnemyHitBox>();

                if (enemyHitbox == null && other.attachedRigidbody != null)
                {
                    enemyHitbox = other.attachedRigidbody.GetComponent<EnemyHitBox>()
                               ?? other.attachedRigidbody.GetComponentInChildren<EnemyHitBox>();
                }
            }

            if (enemyHitbox != null && enemyHitbox.HasPayload)
            {
                var payload = enemyHitbox.Payload;

                if (!healthComponent.IsDie())
                {
                    Debug.Log("damage : " + payload.Damage);

                    bool interrupted = healthComponent.CanBeInterruptedBy(payload.AttackerArmor);
                    Debug.Log($"[PlayerHurtbox] interrupted={interrupted} (attackerArmor={payload.AttackerArmor}, superArmor={healthComponent.superArmor})");

                    healthComponent.TakeDamage(payload.Damage, payload.AttackerArmor);

                    // Only route to DamagedState if the enemy's attack broke player's super armor.
                    // If armor holds, damage still applies but state/inputs are undisturbed.
                    if (interrupted)
                    {
                        
                        playerStateController.TriggerDamaged(payload);
                    }
                }
            }
        }
    }
}
