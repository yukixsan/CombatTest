using UnityEngine;

public class EnemyAirborneDamagedState : EnemyBaseState
{
    private float damageTimer;

    private HitboxPayload pendingPayload;
    private bool hasPendingPayload;
    private bool pendingIsJuggle;


    public EnemyAirborneDamagedState(EnemyStateController controller) : base(controller) { }

    public void SetPendingKnockback(HitboxPayload payload)
    {
        pendingPayload = payload;
        hasPendingPayload = true;
        
    }

    public override void OnEnter()
    {
        Debug.Log("EnemyAirborneDamagedState: OnEnter() called");
        damageTimer = controller.damagedDuration;
        rb.excludeLayers = LayerMask.GetMask("Player");

        if (anim != null) anim.SetTrigger("airDamaged");

        if (hasPendingPayload)
        {
            ApplyKnockbackImpulse(pendingPayload);
            hasPendingPayload = false;
        }
    }

    // Re-entry path: called by EnemyStateController.TriggerDamaged when already
    // in this state, so repeated air-juggle hits refresh the timer and re-apply
    // knockback instead of being swallowed by the SwitchState no-op guard.
    public void Reset(HitboxPayload payload)
    {
        damageTimer = controller.damagedDuration;
        if (anim != null) anim.SetTrigger("airDamaged");
        ApplyKnockbackImpulse(payload);
    }

    private void ApplyKnockbackImpulse(HitboxPayload payload)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        EnemyHitReaction.ApplyKnockback(payload, rb);
    }

    public override void OnUpdate()
    {
        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            // Always hand off to AirborneState — it alone decides Idle vs staying airborne.
            controller.SwitchState(controller.AirborneState);
        }
    }

    public override void OnExit()
    {
        // Restore normal collision layers when leaving this state.
        rb.excludeLayers = 0;
    }
}