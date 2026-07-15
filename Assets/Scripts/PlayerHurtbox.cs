using UnityEngine;

public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private PlayerStateController playerStateController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            var enemyHitbox = other.transform.root.GetComponentInChildren<EnemyHitBox>();
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
