using UnityEngine;

public class SkillObject : MonoBehaviour
{
    protected int damage;
    protected float knockback;
    [SerializeField] private PlayerHitbox _hitbox;

    public virtual void Initialize(PlayerSkillData data, Transform player)
    {
        transform.position = player.position + data.spawnOffset;
        transform.rotation = player.rotation;

        if (data.attachToPlayer)
            transform.SetParent(player);
        if (_hitbox != null)
        {
             var payload = new HitboxPayload
            {
                Damage = data.damage,
                KnockbackForce = data.knockbackForce,
                KnockbackDirection = data.knockbackDirection
            };

            _hitbox.Initialize(player);
            _hitbox.SetPayload(payload);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Hit {other.name} for {damage} damage!");
        // TODO: integrate Health later
    }
}
