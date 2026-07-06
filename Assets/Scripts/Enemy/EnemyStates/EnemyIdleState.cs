using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateController controller) : base(controller) { }

    private float chaseCDTimer;
    public override void OnEnter()
    {
        Debug.Log("EnemyIdleState: OnEnter() called");  
        movement.StopMovement();
        chaseCDTimer = controller.idleToChaseDelay;
    }

    public override void OnUpdate()
    {
        Debug.Log("EnemyIdleState: OnUpdate() called");
        if (controller.target == null) return;

        if (chaseCDTimer > 0f) //check if the cooldown timer is still running
        {
            chaseCDTimer -= Time.deltaTime;
            Debug.Log($"EnemyIdleState: chaseCDTimer = {chaseCDTimer}");
            return;
        }

        float dist = UnityEngine.Vector3.Distance(controller.transform.position, controller.target.position);
        if (dist <= controller.chaseRange)
        {
            controller.SwitchState(controller.ChaseState);
        }
    }
}
