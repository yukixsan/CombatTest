using Unity.VisualScripting;
using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    [SerializeField] private EnemyCombat _enemyCombat;
    [SerializeField] private HealthComponent healthComponent;
   
    private void OnTriggerEnter(Collider other)
    {
        print("collide");
        var hitbox = other.GetComponent<PlayerHitbox>();
        if (hitbox != null && hitbox.HasPayload)
        {
            _enemyCombat.TakeHit(hitbox.Payload);
            healthComponent.TakeDamage(hitbox.Payload.Damage);
        }
    }
 
    public void TryTakeHit(PlayerHitbox hitbox)
    {
        if (hitbox.HasPayload)
        {
            _enemyCombat.TakeHit(hitbox.Payload);
            healthComponent.TakeDamage(hitbox.Payload.Damage);
        }
    }

    
}
