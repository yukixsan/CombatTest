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

    public void TakeHit(HitboxPayload payload)
    {
        print("enemy hit");
        // Damage
        currentHealth -= payload.Damage;
        Debug.Log($"{name} took {payload.Damage} dmg. HP: {currentHealth}/{maxHealth}");

        // Knockback
        float facingX = Mathf.Sign(transform.position.x - payload.attacker.position.x);

        Vector3 knockback = new Vector3(
                   payload.KnockbackForce * facingX,
                   payload.LaunchForce * payload.LaunchDir,
                   0
               );

        rb.AddForce(knockback, ForceMode.Impulse);

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
