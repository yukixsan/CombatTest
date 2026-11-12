using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float fallInterruptDuration = 0.08f;
    [SerializeField] private float fallStartDelay = 0.12f;
    private float tempFallMult;
    private float fallInterruptTimer;
    private float fallStartTimer;
    private bool isAirborneFallActive;
    private Vector3 moveVelocity;
    public bool IsMoving => new Vector2(moveVelocity.x, moveVelocity.z).sqrMagnitude > 0.001f;

    [Header("Knockback Deceleration")]
    [SerializeField] private float groundKnockbackDecel = 38f;
    [SerializeField] private float airKnockbackDecel = 22f;

    [Header("Ground")]
    public Transform groundCheckPoint;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    public bool IsGrounded { get; private set; }

    [Header("Enemy-Enemy Blocking")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float blockCheckRadius = 0.4f;
    [SerializeField] private float blockCheckDistance = 0.5f;
    [SerializeField] private Transform bodyCenter; // chest-height point on the capsule, assign in inspector

    [SerializeField] private Rigidbody rb;

    private float _lockedDepth;

    private void Start()
    {
        if (rb != null)
        {
            // If enemy is oriented along Z (e.g. Y angle ~ 90 or 270), freeze X position; otherwise freeze Z position.
            float yAngle = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.y));
            bool isZStreet = yAngle > 45f && yAngle < 135f;

            _lockedDepth = isZStreet ? transform.position.x : transform.position.z;

            RigidbodyConstraints posFreeze = isZStreet 
                ? RigidbodyConstraints.FreezePositionX 
                : RigidbodyConstraints.FreezePositionZ;

            rb.constraints = posFreeze | RigidbodyConstraints.FreezeRotation;
        }
    }


    private void FixedUpdate()
    {
        UpdateGroundCheck();
        ApplyFallMultiplier();
    }

    public void SetMoveVelocity(Vector3 velocity) => moveVelocity = velocity;
    public void StopMovement() => moveVelocity = Vector3.zero;

    public void InterruptFall()
    {
        fallInterruptTimer = Mathf.Max(fallInterruptTimer, fallInterruptDuration);
        fallStartTimer = Mathf.Max(fallStartTimer, fallStartDelay);
    }

    public void BeginAirborneFall()
    {
        isAirborneFallActive = true;
        fallStartTimer = fallStartDelay;
    }

    public void StopAirborneFall()
    {
        isAirborneFallActive = false;
        fallStartTimer = 0f;
    }

    public void SetFallMult(float speed)
    {
        tempFallMult = fallMultiplier;
        fallMultiplier = speed;
    }

    public void ResetFallMult()
    {
        fallMultiplier = tempFallMult;
    }

    public float FallMultiplier => fallMultiplier;

    public void ApplyKnockbackDeceleration()
    {
        if (rb == null) return;

        float decel = IsGrounded ? groundKnockbackDecel : airKnockbackDecel;
        Vector3 vel = rb.linearVelocity;
        Vector2 horizontalVel = new Vector2(vel.x, vel.z);
        if (horizontalVel.sqrMagnitude > 0.0001f)
        {
            horizontalVel = Vector2.MoveTowards(horizontalVel, Vector2.zero, decel * Time.fixedDeltaTime);
            vel.x = horizontalVel.x;
            vel.z = horizontalVel.y;
            rb.linearVelocity = vel;
        }
    }

    public void UpdateGroundCheck()
    {
        if (groundCheckPoint == null) return;
        IsGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckDistance, groundLayer);
    }

    private void ApplyFallMultiplier()
    {
        if (!isAirborneFallActive)
        {
            return;
        }

        if (fallInterruptTimer > 0f)
        {
            fallInterruptTimer -= Time.fixedDeltaTime;
            return;
        }

        if (fallStartTimer > 0f)
        {
            fallStartTimer -= Time.fixedDeltaTime;
            return;
        }

        if (!IsGrounded && rb != null && rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        }
    }

    public void ApplyMovement()
    {
        Vector3 intendedMove = moveVelocity * Time.fixedDeltaTime;

        // Enforce 2.5D depth constraint on enemy movement
        float yAngle = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.y));
        bool isZStreet = yAngle > 45f && yAngle < 135f;
        if (isZStreet)
        {
            intendedMove.x = 0f;
        }
        else
        {
            intendedMove.z = 0f;
        }

        Vector3 horizontalMove = new Vector3(intendedMove.x, 0f, intendedMove.z);

        if (horizontalMove.sqrMagnitude > 0.0001f && bodyCenter != null)
        {
            Vector3 dir = horizontalMove.normalized;
            bool blocked = Physics.SphereCast(
                bodyCenter.position,
                blockCheckRadius,
                dir,
                out _,
                blockCheckDistance,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            if (blocked)
            {
                intendedMove.x = 0f;
                intendedMove.z = 0f;
            }
        }

        if (rb != null)
        {
            Vector3 targetPos = rb.position + intendedMove;
            if (isZStreet)
            {
                targetPos.x = _lockedDepth;
            }
            else
            {
                targetPos.z = _lockedDepth;
            }

            rb.MovePosition(targetPos);
        }
    }

    public void Flip(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = dirX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    public void SetExcludeLayers(LayerMask mask) => rb.excludeLayers = mask;

}
