using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public EnemyAttackState(EnemyStateController controller) : base(controller) { }

    private  float attackDelay = 1f;
    private float attackTimer;
    public override void OnEnter()
    {
        base.OnEnter();
        movement.StopMovement();
        Debug.Log("EnemyAttackState: OnEnter() called");
        attackTimer = attackDelay;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (attackTimer > 0f) //check if the cooldown timer is still running
        {
            attackTimer -= Time.deltaTime;
            //Debug.Log($"EnemyIdleState: chaseCDTimer = {chaseCDTimer}");
            return;
        }
        Debug.Log("EnemyAttackState timer finished called");
        controller.SwitchState(controller.IdleState);
    }
}
