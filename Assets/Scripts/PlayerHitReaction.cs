using UnityEngine;

public static class PlayerHitReaction 
{
    public static Vector3 ResolveKnockbackVelocity(HitboxPayload payload, Vector3 targetPosition)
    {
        float facingX = Mathf.Sign(targetPosition.x - payload.attacker.position.x);
        if (facingX == 0f) facingX = 1f;

        float launchY = payload.LaunchForce * payload.LaunchDir;

        return new Vector3(payload.KnockbackForce * facingX, launchY, 0f);
    }
}
