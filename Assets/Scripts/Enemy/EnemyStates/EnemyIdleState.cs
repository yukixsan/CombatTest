using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyStateController controller) : base(controller) { }

    private float chaseCDTimer;
    public override void OnEnter()
    {
        Debug.Log("EnemyIdleState: OnEnter() called");  
        anim.Play("Idle");
        
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        rb.useGravity = true;
        movement.StopMovement();
        chaseCDTimer = controller.idleToChaseDelay;

        if (controller.target != null)
        {
            Vector3 dir = (controller.target.position - controller.transform.position).normalized;
            movement.Flip(dir.x);
        }
    }

    public override void OnUpdate()
    {
        //Debug.Log("EnemyIdleState: OnUpdate() called");
         if (!movement.IsGrounded)
        {
            controller.SwitchState(controller.AirborneState);
            return;
        }

        if (controller.target == null) return;

        Vector3 dir = (controller.target.position - controller.transform.position).normalized;
        movement.Flip(dir.x);

        if (chaseCDTimer > 0f)
        {
            chaseCDTimer -= Time.deltaTime;
            return;
        }

        float dist = UnityEngine.Vector3.Distance(controller.transform.position, controller.target.position);

        if (dist <= controller.chaseRange)
        {
            controller.SwitchState(controller.ChaseState);
        }
        if (dist <= controller.stopDistance)
        {
            controller.SwitchState(controller.AttackState);
        }
    }

    public override void OnExit()
    {
        Debug.Log("EnemyIdleState: OnExit() called");
    }
}
