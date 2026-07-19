using UnityEngine;

public class EnemyDieState : EnemyBaseState
{
    private LayerMask _originalExcludeLayers;

    public EnemyDieState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log("EnemyDeadState: OnEnter() called");

        // Stop horizontal motion, but allow the rigidbody to fall under gravity.
        movement.StopMovement();

        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        // Restore physics-driven behavior so the enemy can drop naturally.
        rb.isKinematic = false;
        rb.useGravity = true;

        // Disable player collision while preserving ground collision.
        _originalExcludeLayers = rb.excludeLayers;
        rb.excludeLayers = LayerMask.GetMask("Player");

        // Disable hitbox so no further attacks can be initiated/land.
        controller.Hitbox?.Deactive();

        if (anim != null) anim.SetTrigger("dead");
    }

    // No OnUpdate override — intentionally does nothing, so this state
    // performs no further checks (no distance checks, no ground checks,
    // no transitions). Dead is terminal until external respawn/destroy logic.

    public override void OnExit()
    {
        rb.excludeLayers = _originalExcludeLayers;
    }
}
