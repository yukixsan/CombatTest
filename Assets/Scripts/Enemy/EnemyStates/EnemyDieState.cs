using UnityEngine;

public class EnemyDieState : EnemyBaseState
{
    public EnemyDieState(EnemyStateController controller) : base(controller) { }

     private Collider _bodyCollider;
    public override void OnEnter()
    {
        Debug.Log("EnemyDeadState: OnEnter() called");

        // Stop all motion and physics reactions
        movement.StopMovement();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Disable main body collision
        if (_bodyCollider == null)
            _bodyCollider = rb.GetComponent<Collider>();
        if (_bodyCollider != null)
            _bodyCollider.enabled = false;

        // Disable hitbox so no further attacks can be initiated/land
        controller.Hitbox?.Deactive();

        if (anim != null) anim.SetTrigger("dead");
    }

    // No OnUpdate override — intentionally does nothing, so this state
    // performs no further checks (no distance checks, no ground checks,
    // no transitions). Dead is terminal until external respawn/destroy logic.

    public override void OnExit()
    {
        // Only relevant if a future respawn/pool system re-enters this enemy.
        if (_bodyCollider != null)
            _bodyCollider.enabled = true;
        rb.isKinematic = false;
    }

}
