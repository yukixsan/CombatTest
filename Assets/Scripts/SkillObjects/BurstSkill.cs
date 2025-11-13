using Unity.VisualScripting;
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
        
        //_hitboxPos.transform.localScale = flip;
        // Give it a forward velocity (assuming right = forward in 2D)
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir =  Vector2.right * _facing;
            rb.linearVelocity = dir * speed;
        }

        // Destroy after lifetime expires
        Destroy(gameObject, lifetime);
    }
}
