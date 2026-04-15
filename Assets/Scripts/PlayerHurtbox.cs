using UnityEngine;

public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;

    private void OnTriggerEnter(Collider other)
    {
        print("collide");
        if (other.gameObject.layer == 6)
        {
            var hit = other.transform.root.GetComponent<EnemyCombat>();
            if(hit != null)
            {
                if (!healthComponent.IsDie())
                {
                    healthComponent.TakeDamage(hit.GetDamage());
                }
            }
        }
    }
}
