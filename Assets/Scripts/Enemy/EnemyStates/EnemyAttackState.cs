using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
     private float attackTimer;
    private bool isAttacking;

    public EnemyAttackState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log("EnemyAttackState: OnEnter() called");
        movement.StopMovement();
        StartAttack();
    }

    
    private void StartAttack()
    {
         Debug.Log("EnemyAttackState: Attack started");
        isAttacking = true;
        attackTimer = controller.attackDuration;
        controller.canAttack = false;

        if (anim != null) anim.SetTrigger("Attack");

        var payload = new HitboxPayload(
            controller.attackDamage,
            controller.attackArmor,
            controller.attackKnockbackForce,
            controller.attackLaunchForce,
            controller.attackLaunchDir,
            controller.attackHitstopDuration,
            controller.transform,
            controller.attackVFXIndex
        );
        controller.Hitbox?.SetPayload(payload);

        //controller.Hitbox?.Active();
    }

    public override void OnUpdate()
    {
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                EndAttack();
            }
            return; // let attack finish before re-checking distance
        }

        // Cooldown tick — happens after attack finishes, before leaving state
        if (controller.attackCooldownTimer > 0f)
        {
            controller.attackCooldownTimer -= Time.deltaTime;
            if (controller.attackCooldownTimer <= 0f)
            {
                controller.canAttack = true;
            }
            return;
        }

        // Attack + cooldown both done — re-check distance now
        if (controller.target == null)
        {
            controller.SwitchState(controller.IdleState);
            return;
        }

        float dist = Vector3.Distance(controller.transform.position, controller.target.position);

        if (dist > controller.stopDistance)
        {
            controller.SwitchState(controller.ChaseState);
        }
        else if (controller.canAttack)
        {
            StartAttack(); // still in range, cooldown cleared — attack again
        }
    }

    private void EndAttack()
    {
        Debug.Log("EnemyAttackState: Attack finished");
        isAttacking = false;
        controller.Hitbox?.Deactive();
        controller.attackCooldownTimer = controller.attackCooldown;
    }

    public override void OnExit()
    {
        // Unconditional cleanup — covers interruption by DamagedState mid-attack,
        // not just the natural end-of-duration path.
        if (isAttacking)
        {
            controller.Hitbox?.Deactive();
        }
        isAttacking = false;
    }
}
