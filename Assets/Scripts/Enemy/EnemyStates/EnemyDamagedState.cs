using UnityEngine;

public class EnemyDamagedState : EnemyBaseState
{
    private float damageTimer;

    private HitboxPayload pendingPayload;
    private bool hasPendingPayload;

    public EnemyDamagedState(EnemyStateController controller) : base(controller) { }

    public void SetPendingKnockback(HitboxPayload payload)
    {
        pendingPayload = payload;
        hasPendingPayload = true;
    }

    public override void OnEnter()
    {
        damageTimer = controller.damagedDuration;
        movement.StopMovement();

        if (anim != null) anim.SetTrigger("damage");

        if (hasPendingPayload)
        {
            ApplyKnockbackImpulse(pendingPayload);
            hasPendingPayload = false;
        }
    }

    private void ApplyKnockbackImpulse(HitboxPayload payload)
    {

        rb.isKinematic = false;
        EnemyHitReaction.ApplyKnockback(payload, rb);
    }

    public override void OnUpdate()
    {
        Debug.Log("EnemyDamagedState: OnUpdate() called");
        damageTimer -= Time.deltaTime;
        if(!movement.IsGrounded)
        {
            return; // Wait until the enemy is grounded before switching states
        }
        if (damageTimer <= 0f)
        {
            controller.SwitchState(controller.IdleState);
        }
    }

    public override void OnExit()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}
