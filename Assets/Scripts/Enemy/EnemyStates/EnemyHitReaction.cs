using UnityEngine;

public static class EnemyHitReaction 
{
    private const float juggleLaunchScale = 0.1f;
    // private static float juggleMaxHeightScale = 1.5f;

    public static Vector3 ResolveKnockbackVelocity(HitboxPayload payload, Vector3 targetPosition, Rigidbody targetRb, bool isJuggle)
{
   float facingX = Mathf.Sign(targetPosition.x - payload.attacker.position.x);
    if (facingX == 0f) facingX = 1f;

    float launchY = payload.LaunchForce * payload.LaunchDir;
    float currentVelY = targetRb.linearVelocity.y;

    float desiredY;
    if (isJuggle)
    {
        float juggleTargetY = launchY * juggleLaunchScale;
        // Never pull velocity DOWN to reach the juggle target — only top up
        // if the enemy has already decayed below it. Prevents the downward
        // "yank" when a second hit lands while still rising from the first.
        desiredY = Mathf.Max(currentVelY, juggleTargetY);
    }
    else
    {
        desiredY = launchY;
    }

    Vector3 desiredVelocity = new Vector3(
        payload.KnockbackForce * facingX,
        desiredY,
        0f
    );

    return desiredVelocity - targetRb.linearVelocity;
}

public static void ApplyKnockback(HitboxPayload payload, Rigidbody targetRb, bool isJuggle = false)
{
     float facingX = Mathf.Sign(targetRb.transform.position.x - payload.attacker.position.x);
    if (facingX == 0f) facingX = 1f;

    float launchY = payload.LaunchForce * payload.LaunchDir;
    float currentVelY = targetRb.linearVelocity.y;

    float finalY;
    if (currentVelY > 0.1f)
    {
        // Already airborne/rising — REDUCE the effective launch instead of
        // granting a fresh full one, and never let it stack additively.
        // Hold at the higher of (current velocity) or (reduced target) —
        // this caps height, it doesn't add to it.
        float reducedTarget = launchY * juggleLaunchScale;
        finalY = Mathf.Max(currentVelY, reducedTarget);
    }
    else
    {
        // Falling, grounded, or at rest — fresh full launch.
        finalY = launchY;
    }

    targetRb.linearVelocity = new Vector3(payload.KnockbackForce * facingX, finalY, 0f);
}
}
