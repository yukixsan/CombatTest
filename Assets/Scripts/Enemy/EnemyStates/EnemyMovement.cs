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
    public bool IsMoving => Mathf.Abs(moveVelocity.x) > 0.01f;

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

        if (intendedMove.x != 0f && bodyCenter != null)
        {
            Vector3 dir = new Vector3(Mathf.Sign(intendedMove.x), 0f, 0f);
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
            }
        }

        if (rb != null)
        {
            rb.MovePosition(transform.position + intendedMove);
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
