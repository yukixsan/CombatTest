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
        Debug.Log("EnemyDamagedState: OnEnter() called");
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
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.excludeLayers = LayerMask.GetMask("Player");
        EnemyHitReaction.ApplyKnockback(payload, rb, isJuggle : false);
    }

    public override void OnUpdate()
    {
        Debug.Log("EnemyDamagedState: OnUpdate() called");
        damageTimer -= Time.deltaTime;
        if(!movement.IsGrounded)
        {
            controller.SwitchState(controller.AirborneDamagedState);
            return;
        }
        if (damageTimer <= 0f /*&& movement.IsGrounded*/)
        {
            controller.SwitchState(controller.IdleState);
        }
    }

    public override void OnExit()
    {
        if (controller.IsAirborne || controller.IsAirborneDamaged) return;
        rb.excludeLayers = 0; // restore normal collision layers

        //rb.isKinematic = true;
    }
    public void Reset(HitboxPayload payload)
    {
        damageTimer = controller.damagedDuration;
        if (anim != null) anim.SetTrigger("damage");
        ApplyKnockbackImpulse(payload);
    }
}
