using UnityEngine;

public class EnemyAirborneState : EnemyBaseState
{
    public EnemyAirborneState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log("EnemyAirborneState: OnEnter() called");
        anim.SetBool("fall", true);
        // anim.SetTrigger("fall");
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
    public override void OnExit()
    {
        anim.SetBool("fall", false);
    }
}