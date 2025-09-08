using Unity.VisualScripting;
using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    [SerializeField] private EnemyCombat _enemyCombat;

    private void OnTriggerEnter(Collider other)
    {
        print("collide");
        var hitbox = other.GetComponent<PlayerHitbox>();
        if (hitbox != null && hitbox.HasPayload)
        {
            _enemyCombat.TakeHit(hitbox.Payload);
        }
    }
    public void TryTakeHit(PlayerHitbox hitbox)
    {
        if (hitbox.HasPayload)
        {
            _enemyCombat.TakeHit(hitbox.Payload);
        }
    }

    
}
