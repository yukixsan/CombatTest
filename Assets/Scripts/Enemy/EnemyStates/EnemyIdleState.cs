using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        movement.StopMovement();
    }

    public override void OnUpdate()
    {
        if (controller.target == null) return;

        float dist = UnityEngine.Vector3.Distance(controller.transform.position, controller.target.position);
        if (dist <= controller.chaseRange)
        {
            controller.SwitchState(controller.ChaseState);
        }
    }
}
