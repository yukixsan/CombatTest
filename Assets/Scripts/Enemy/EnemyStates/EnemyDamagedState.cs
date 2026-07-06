using UnityEngine;

public class EnemyDamagedState : EnemyBaseState
{
    [Header("Tuning — set via inspector on EnemyStateController or hardcode here for now")]
    private float damagedDuration = 0.3f;
    private float timer;

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
        timer = damagedDuration;
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
        float facingX = Mathf.Sign(controller.transform.position.x - payload.attacker.position.x);
        Vector3 knockback = new Vector3(payload.KnockbackForce * facingX, 0f, 0f);

        // Rigidbody must not be kinematic for AddForce to have any effect (Unity docs:
        // "Force can only be applied to an active Rigidbody... cannot be kinematic").
        rb.isKinematic = false;
        rb.AddForce(knockback, ForceMode.Impulse);
    }

    public override void OnUpdate()
    {
        Debug.Log("EnemyDamagedState: OnUpdate() called");
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            controller.SwitchState(
                controller.target != null ? controller.ChaseState : controller.IdleState
            );
        }
    }

    public override void OnExit()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}
