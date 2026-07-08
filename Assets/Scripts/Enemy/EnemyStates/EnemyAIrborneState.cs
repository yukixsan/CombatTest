using UnityEngine;

public class EnemyAirborneState : EnemyBaseState
{
    public EnemyAirborneState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log("EnemyAirborneState: OnEnter() called");
        rb.isKinematic = false;
        rb.useGravity = true;
        movement.StopMovement();
    }

    public override void OnUpdate()
    {
        if (movement.IsGrounded)
        {
            controller.SwitchState(controller.IdleState);
        }
    }
}