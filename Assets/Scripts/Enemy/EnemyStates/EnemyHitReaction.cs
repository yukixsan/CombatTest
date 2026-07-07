using UnityEngine;

public static class EnemyHitReaction 
{
    public static Vector3 ResolveKnockbackVelocity(HitboxPayload payload, Vector3 targetPosition, Rigidbody targetRb)
    {
        float facingX = Mathf.Sign(targetPosition.x - payload.attacker.position.x);
        if (facingX == 0f) facingX = 1f; // guard Mathf.Sign(0) == 0 on exact overlap

        Vector3 desiredVelocity = new Vector3(
            payload.KnockbackForce * facingX,
            payload.LaunchForce * payload.LaunchDir,
            0f
        );

        // Cancel existing velocity first so repeat hits don't stack on top of
        // residual motion (chase velocity, previous knockback not yet decayed).
        Vector3 velocityDelta = desiredVelocity - targetRb.linearVelocity;
        return velocityDelta;
    }

    // Applies the resolved knockback directly — call site doesn't need to
    // know the VelocityChange/cancel-residual details.
    public static void ApplyKnockback(HitboxPayload payload, Rigidbody targetRb)
    {
        Vector3 velocityDelta = ResolveKnockbackVelocity(payload, targetRb.transform.position, targetRb);
        targetRb.AddForce(velocityDelta, ForceMode.VelocityChange);
    }
}
