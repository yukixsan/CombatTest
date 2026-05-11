using UnityEngine;

public class SkillObject : MonoBehaviour
{
    protected HitboxPayload payload;
    protected float _facing = 1f;
    [SerializeField] protected Transform _model;
    [SerializeField] private PlayerHitbox _hitbox;

    public virtual void Initialize(PlayerSkillData data, Transform player)
    {
        _facing = Mathf.Sign(player.localScale.x);
        Vector3 actualOffset = data.spawnOffset;
        actualOffset.x *= _facing;

        transform.position = player.position + actualOffset;
        // Don't apply player rotation - keep skill object's own z-rotation
        // Mirror rotation when facing left
        if (_facing < 0)
        {
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = -eulerAngles.z;
            transform.eulerAngles = eulerAngles;
        }
        // optional: flip visuals if assigned
        if (_model != null)
        {
            Vector3 scale = _model.localScale;
            scale.x = Mathf.Abs(scale.x) * _facing;  // Always set explicitly based on facing
            _model.localScale = scale;
        }
        
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
                player,
                data.VFXindex    // attacker reference
            );

            _hitbox.Initialize(player);
            _hitbox.SetPayload(payload);
        }
    }

    
}
