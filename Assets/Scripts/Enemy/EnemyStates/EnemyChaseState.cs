using UnityEngine;

public class EnemyChaseState : EnemyBaseState
{
        public EnemyChaseState(EnemyStateController controller) : base(controller) { }

    public override void OnFixedUpdate()
    {
        Debug.Log("EnemyChaseState: OnFixedUpdate() called");
        if (controller.target == null)
        {
            controller.SwitchState(controller.IdleState);
            return;
        }

        float dist = Vector3.Distance(controller.transform.position, controller.target.position);

        if (dist > controller.stopDistance)
        {
            Vector3 dir = (controller.target.position - controller.transform.position).normalized;
            movement.SetMoveVelocity(new Vector3(dir.x * movement.moveSpeed, 0, 0));
            movement.ApplyMovement();
            movement.Flip(dir.x);
        }
        else
        {
            movement.StopMovement();
            controller.SwitchState(controller.IdleState);
        }

        if (dist > controller.detectRange)
        {
            controller.SwitchState(controller.IdleState);
        }
    }

}
