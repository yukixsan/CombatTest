using UnityEngine;

public class SkillObject : MonoBehaviour
{
    protected HitboxPayload payload;
    protected float _facing = 1f;
    [SerializeField] protected Transform _model;
    [SerializeField] protected PlayerHitbox _hitbox;

    public virtual void Initialize(PlayerSkillData data, Transform player)
    {
        _facing = Mathf.Sign(player.localScale.x);

        var mover = player.GetComponentInParent<PlayerMovement>();
        Transform rootTransform = mover != null ? mover.transform : player.root;
        Vector3 streetForward = mover != null ? mover.StreetForward : (Mathf.Abs(rootTransform.forward.z) > 0.5f ? Vector3.forward : Vector3.right);
        streetForward.y = 0f;
        if (streetForward.sqrMagnitude > 0.001f) streetForward.Normalize();

        // 1. Position calculation with street-aware offset
        Vector3 actualOffset = data.spawnOffset;
        actualOffset.x *= _facing;
        Vector3 worldOffset = rootTransform.TransformDirection(actualOffset);
        transform.position = rootTransform.position + worldOffset;

        // 2. Base prefab rotation preservation (e.g. Z = -35 for Burst Ring)
        // Street rotation aligns to current road angle, facing flips 180 around Y when facing left
        Quaternion streetRot = rootTransform.rotation;
        Quaternion facingRot = _facing < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        Quaternion basePrefabRot = data.skillPrefab != null ? data.skillPrefab.transform.rotation : Quaternion.identity;
        transform.rotation = streetRot * facingRot * basePrefabRot;

        // Visual model scaling (ensure root Rigidbody is never negatively scaled)
        if (_model != null && _model != transform)
        {
            Vector3 scale = _model.localScale;
            scale.x = Mathf.Abs(scale.x);
            _model.localScale = scale;
        }
        
        if (data.attachToPlayer)
            transform.SetParent(player);
        if (_hitbox != null)
        {
            // Build payload for this skill
            payload = new HitboxPayload(
                data.damage,
                data.attackArmor,
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
