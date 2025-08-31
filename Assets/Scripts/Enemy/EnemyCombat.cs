using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
   [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [SerializeField]private Rigidbody rb;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeHit(HitboxPayload payload, Transform attacker)
    {
        print("enemy hit");
        // Damage
        currentHealth -= payload.Damage;
        Debug.Log($"{name} took {payload.Damage} dmg. HP: {currentHealth}/{maxHealth}");

        // Knockback
        // Default knockback direction from payload

       
        Vector3 kb = payload.KnockbackDirection * payload.KnockbackForce;
        rb.AddForce(kb, ForceMode.Impulse);

        // TODO: Apply stun/animation if needed
        // e.g. StartCoroutine(Hitstun(payload.HitstunDuration));

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
        // TODO: play death anim, disable AI, etc.
        Destroy(gameObject);
    }
}
