using UnityEngine;

public class BurstSkill : SkillObject
{

    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private Rigidbody2D rb;

    public override void Initialize(PlayerSkillData data, Transform player)
    {
        base.Initialize(data, player);

        // Give it a forward velocity (assuming right = forward in 2D)
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = player.localScale.x > 0 ? Vector2.right : Vector2.left;
            rb.linearVelocity = dir * speed;
        }

        // Destroy after lifetime expires
        Destroy(gameObject, lifetime);
    }
}
