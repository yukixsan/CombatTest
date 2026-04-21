using UnityEngine;

public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Layer : " + other.gameObject.layer);
        if (other.gameObject.layer == 9)
        {
            print("collide : "+ other.transform.root.GetComponent<EnemyStateAI>().gameObject.name);
            var hit = other.transform.root.GetComponent<EnemyStateAI>();
            if (hit != null)
            {
                Debug.Log("damage : "+ hit.GetDamage());
                if (!healthComponent.IsDie())
                {
                    Debug.Log("damage : "+ hit.GetDamage());
                    healthComponent.TakeDamage(hit.GetDamage(), hit.GetPoiseDamage());
                }
            }
        }
    }
}
