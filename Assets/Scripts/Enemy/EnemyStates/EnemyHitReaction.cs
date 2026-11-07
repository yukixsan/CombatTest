using UnityEngine;

public static class EnemyHitReaction 
{
    public static void ApplyKnockback(
        HitboxPayload payload, 
        Rigidbody targetRb, 
        bool isGrounded, 
        float maxJuggleHeight, 
        float juggleLiftVelocity, 
        LayerMask groundLayer)
    {
        var movement = targetRb.GetComponent<EnemyMovement>();
        movement?.InterruptFall();

        float facingX = Mathf.Sign(targetRb.transform.position.x - payload.attacker.position.x);
        if (facingX == 0f) facingX = 1f;

        float launchY = payload.LaunchForce * payload.LaunchDir;
        float currentVelY = targetRb.linearVelocity.y;

        float finalY;

        // Downward attack (spike / slam)
        if (payload.LaunchDir < 0 && payload.LaunchForce > 0f)
        {
            finalY = -payload.LaunchForce;
        }
        // Grounded hit
        else if (isGrounded)
        {
            finalY = launchY; // Full launch from ground, or 0 for horizontal-only hit
        }
        // Airborne hit (even if enemy is falling, hit cleanly catches and applies vertical force)
        else
        {
            // Upward launch attack while in the air
            if (payload.LaunchForce > 0f && payload.LaunchDir > 0)
            {
                // Measure current height above ground
                float currentHeight = 0f;
                Vector3 origin = movement != null && movement.groundCheckPoint != null 
                    ? movement.groundCheckPoint.position 
                    : targetRb.position;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 40f, groundLayer))
                {
                    currentHeight = hit.distance;
                }

                if (currentHeight >= maxJuggleHeight)
                {
                    // At or above max juggle ceiling: sustain the air combo at ceiling instead of rocketing to space
                    finalY = juggleLiftVelocity;
                }
                else
                {
                    // Below ceiling: full upward launch applies, cleanly overriding any falling velocity
                    finalY = launchY;
                }
            }
            // Non-launch attack in the air (light combo strike, neutral hit)
            else
            {
                // Catch falling enemy and suspend them in the air during combo
                finalY = juggleLiftVelocity;
            }
        }

        targetRb.linearVelocity = new Vector3(payload.KnockbackForce * facingX, finalY, 0f);
    }
}
