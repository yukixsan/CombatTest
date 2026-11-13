using UnityEngine;

public static class PlayerHitReaction 
{
    public static Vector3 ResolveKnockbackVelocity(HitboxPayload payload, Vector3 targetPosition)
    {
        Vector3 streetForward = Vector3.right;
        if (payload.attacker != null)
        {
            var mover = payload.attacker.GetComponentInParent<PlayerMovement>();
            if (mover != null)
            {
                streetForward = mover.StreetForward;
            }
        }

        Vector3 attackerPos = payload.attacker != null ? payload.attacker.position : targetPosition - streetForward;
        Vector3 diff = targetPosition - attackerPos;
        float facingSign = Mathf.Sign(Vector3.Dot(diff, streetForward));
        if (facingSign == 0f) facingSign = 1f;

        float launchY = payload.LaunchForce * payload.LaunchDir;
        Vector3 horizontalForce = streetForward * (payload.KnockbackForce * facingSign);

        return new Vector3(horizontalForce.x, launchY, horizontalForce.z);
    }
}
