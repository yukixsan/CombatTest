using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
     [Header("Movement")]
    public float moveSpeed = 3f;
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

    // private void Awake()
    // {
    //     rb = GetComponent<Rigidbody>();
    // }

    public void SetMoveVelocity(Vector3 velocity) => moveVelocity = velocity;
    public void StopMovement() => moveVelocity = Vector3.zero;

    public void UpdateGroundCheck()
    {
        if (groundCheckPoint == null) return;
        IsGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckDistance, groundLayer);
    }

    public void ApplyMovement()
    {
         Vector3 intendedMove = moveVelocity * Time.fixedDeltaTime;

        if (intendedMove.x != 0f)
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

        rb.MovePosition(transform.position + intendedMove);
    }

    public void Flip(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = dirX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    public void SetExcludeLayers(LayerMask mask) => rb.excludeLayers = mask;

}
