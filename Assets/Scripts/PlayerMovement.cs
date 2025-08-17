using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float _fallMult;
    [SerializeField] private Transform _model;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    [SerializeField]private bool _isGrounded;
    public bool IsMoving { get; private set; }
    [SerializeField]private bool _jumpPressed;

    //State Controller
    [SerializeField] private PlayerStateController _stateController;
    public bool IsGrounded
    {
        get { return _isGrounded; } set {  _isGrounded = value; }
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Generated input class
        inputActions = new PlayerInputActions();

        // Bind Move (A/D or Left/Right)
        inputActions.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        // Bind Jump
        inputActions.Gameplay.Jump.performed += ctx => _jumpPressed = true;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        //Stopping movement
        if (!_stateController.CanMove) // stateController reference here
        {
            // Freeze X movement if not allowed
            Vector3 stopVel = rb.linearVelocity;
            stopVel.x = 0;
            rb.linearVelocity = stopVel;

            IsMoving = false;
            return;
        }

        // Horizontal movement only (2.5D)
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput.x * moveSpeed;
        rb.linearVelocity = velocity;

        // Update IsMoving (ignores tiny floating values)
        IsMoving = Mathf.Abs(moveInput.x) > 0.1f;

        if (_jumpPressed && _isGrounded)
        {
            Vector3 v = rb.linearVelocity;
            v.y = jumpForce; // instant vertical velocity
            rb.linearVelocity = v;
        }
        _jumpPressed = false;
        if (!_isGrounded && rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.down * _fallMult, ForceMode.Acceleration);
        }


    }

    private void LateUpdate()
    {
        HandleModelFlip(moveInput.x);
    }

    private void HandleModelFlip(float moveX)
    {
        if (moveX > 0)
        {
            _model.localScale = new Vector3(1, 1, 1);
        }
        else if (moveX < 0)
        {
            _model.localScale = new Vector3(-1, 1, 1);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
