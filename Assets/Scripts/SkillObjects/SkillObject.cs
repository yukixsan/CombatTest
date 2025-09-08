using UnityEngine;

public class SkillObject : MonoBehaviour
{
    protected HitboxPayload payload;
    [SerializeField] private PlayerHitbox _hitbox;

    public virtual void Initialize(PlayerSkillData data, Transform player)
    {
        transform.position = player.position + data.spawnOffset;
        transform.rotation = player.rotation;

        if (data.attachToPlayer)
            transform.SetParent(player);
        if (_hitbox != null)
        {
            // Build payload for this skill
            payload = new HitboxPayload(
                data.damage,
                data.knockbackForce,
                data.launchForce,
                data.launchDir,
                data.hitstunDuration,
                player // attacker reference
            );

            _hitbox.Initialize(player);
            _hitbox.SetPayload(payload);
        }
    }

    
}
