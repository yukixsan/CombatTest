using UnityEngine;

public class EnemyChaseState : EnemyBaseState
{
    public EnemyChaseState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        base.OnEnter();
        anim.SetBool("walk", true);
         rb.isKinematic = true;
    }
    public override void OnFixedUpdate()
    {
        // Per-physics-step log removed: 7 enemies x 50 Hz = 350 Debug.Log/s (stack trace + GC) caused hitches.
        if (!movement.IsGrounded)
        {
            controller.SwitchState(controller.AirborneState);
            return;
        }

        if (controller.target == null)
        {
            controller.SwitchState(controller.IdleState);
            return;
        }

        float dist = Vector3.Distance(controller.transform.position, controller.target.position);

        if (dist > controller.stopDistance)
        {
            Vector3 diff = controller.target.position - controller.transform.position;
            Vector3 localDiff = controller.transform.InverseTransformDirection(diff);
            float horizontalSign = Mathf.Sign(localDiff.x);
            if (horizontalSign == 0f) horizontalSign = 1f;

            Vector3 moveDir = controller.transform.TransformDirection(new Vector3(horizontalSign, 0f, 0f));
            movement.SetMoveVelocity(moveDir * movement.moveSpeed);
            movement.ApplyMovement();
            movement.Flip(localDiff.x);
        }
        else
        {
            movement.StopMovement();
            controller.SwitchState(controller.AttackState);
        }

        if (dist > controller.detectRange)
        {
            controller.SwitchState(controller.IdleState);
        }
    }
    public override void OnExit()
    {
        anim.SetBool("walk", false);
    }

}
