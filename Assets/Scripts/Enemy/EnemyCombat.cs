using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damage;
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
        Vector3 streetForward = Vector3.right;
        if (payload.attacker != null)
        {
            var mover = payload.attacker.GetComponentInParent<PlayerMovement>();
            if (mover != null)
            {
                streetForward = mover.StreetForward;
            }
        }

        Vector3 attackerPos = payload.attacker != null ? payload.attacker.position : transform.position - streetForward;
        Vector3 diff = transform.position - attackerPos;
        float facing = Mathf.Sign(Vector3.Dot(diff, streetForward));
        if (facing == 0f) facing = 1f;

        Vector3 hForce = streetForward * (payload.KnockbackForce * facing);
        Vector3 knockback = new Vector3(
                   hForce.x,
                   payload.LaunchForce * payload.LaunchDir,
                   hForce.z
               );

        rb.AddForce(knockback, ForceMode.Impulse);

        Vector3 hitPoint = (payload.attacker.position + transform.position) * 0.5f;
        Debug.Log($"SpawnVFX index: {payload.VFXindex}");

        HitVFXManager.Instance.SpawnVFX(payload.VFXindex, hitPoint, Quaternion.identity);
        HitStopManager.Instance.StartHitstop(payload.HitstopDuration);

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

    public float GetDamage()
    {
        return damage;
    }
}
