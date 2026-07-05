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

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetMoveVelocity(Vector3 velocity) => moveVelocity = velocity;
    public void StopMovement() => moveVelocity = Vector3.zero;

    public void UpdateGroundCheck()
    {
        if (groundCheckPoint == null) return;
        IsGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckDistance, groundLayer);
    }

    public void ApplyMovement()
    {
        rb.MovePosition(transform.position + moveVelocity * Time.fixedDeltaTime);
    }

    public void Flip(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = dirX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
