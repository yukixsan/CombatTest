using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log("EnemyIdleState: OnEnter() called");  
        movement.StopMovement();
    }

    public override void OnUpdate()
    {
        Debug.Log("EnemyIdleState: OnUpdate() called");
        if (controller.target == null) return;

        float dist = UnityEngine.Vector3.Distance(controller.transform.position, controller.target.position);
        if (dist <= controller.chaseRange)
        {
            controller.SwitchState(controller.ChaseState);
        }
    }
}
