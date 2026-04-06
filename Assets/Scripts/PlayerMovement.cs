using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float _fallMult;
    [SerializeField] private Transform _model;
    private Vector3 externalVelocity;
    [Header("Dash Settings")]
    private bool isDashing;
    private float dashTimer;
    // [SerializeField]private float dashDuration;
    // [SerializeField] private float dashSpeed;
    private float _activeDashSpeed;
    private float _activeDashDuration;
    private float dashFacing;


    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    [SerializeField]private bool _isGrounded;
    public bool IsMoving { get; private set; }
    // Accumulator fields
    private Vector3 pendingLaunchVelocity = Vector3.zero;
    private bool hasPendingVelocity = false;
    [Header("Jump checks")]
    [SerializeField]private bool _jumpPressed;
    public float VerticalVelocity => rb.linearVelocity.y;
    public bool IsRising => rb.linearVelocity.y > 0.05f;
    public bool IsFalling => rb.linearVelocity.y < -0.05f;

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
        // 1️⃣ GROUND CHECK
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        Vector3 velocity = rb.linearVelocity;

        // 2️⃣ LAUNCH (HIGHEST PRIORITY)
        if (hasPendingVelocity)
        {
            rb.linearVelocity = pendingLaunchVelocity;

            pendingLaunchVelocity = Vector3.zero;
            hasPendingVelocity = false;
            return; // Launch overrides everything for 1 frame
        }

        // 3️⃣ DASH STATE
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            velocity.y = 0; 
            externalVelocity = new Vector3(dashFacing * _activeDashSpeed, 0f, 0f);

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.useGravity = true;
                externalVelocity = Vector3.zero;
            }
        }

        // 4️⃣ BASE MOVEMENT (INPUT LAYER)
        float baseX = 0f;

        bool allowBaseMovement = _stateController.CanMove;

        // Disable input movement if - movement not allowed - OR currently dashing
        if (allowBaseMovement && !isDashing)
        {
            baseX = moveInput.x * moveSpeed;
        }

        velocity.x = baseX + externalVelocity.x;

        rb.linearVelocity = velocity;

        IsMoving = Mathf.Abs(baseX) > 0.1f;

        // 5️⃣ JUMP
        if (_jumpPressed && _isGrounded)
        {
            velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }
        _jumpPressed = false;

        // 6️⃣ FALL MULTIPLIER
        if (!_isGrounded && rb.linearVelocity.y < 0 )
        {
            rb.AddForce(Vector3.down * _fallMult, ForceMode.Acceleration);
        }
    }

    private void LaunchVelocity(Vector3 dir, float strength)
    {
        float facing = Mathf.Sign(_model.localScale.x); 

        // Flip the X direction if facing left
        dir.x *= facing;

        // Normalize and scale

        pendingLaunchVelocity += dir.normalized * strength;
        hasPendingVelocity = true;
    }

    private void LaunchForce(Vector3 dir, float strength)
    {
        float facing = Mathf.Sign(_model.localScale.x);
        Vector3 finalDir = new Vector3(dir.x * facing, dir.y, dir.z);
        rb.AddForce(finalDir.normalized * strength, ForceMode.VelocityChange);
    }

    // ========= Animation Event Presets =========
    public void LaunchUp(float strength) => LaunchVelocity(Vector3.up, strength);
    public void LaunchForward(float strength) => LaunchVelocity(Vector3.right, strength);
    public void LaunchBack(float strength) => LaunchVelocity(Vector3.left, strength);
    public void LaunchDown(float strength) => LaunchVelocity(Vector3.down, strength);

    public void ForceUp(float strength) => LaunchForce(Vector3.up, strength);
    public void ForceForward(float strength) => LaunchForce(Vector3.right, strength);
    public void ForceBack(float strength) => LaunchForce(Vector3.left, strength);
    public void ForceDown(float strength) => LaunchForce(Vector3.down, strength);

#region Dash Presets
    public void setDashSpeed(float speed) => _activeDashSpeed = speed;
    public void setDashDuration(float duration) => _activeDashDuration = duration;
    
    public void ForceDashForward()
    {
        if (isDashing) return; // Prevent overlapping dashes

        isDashing = true;
        dashTimer = _activeDashDuration;
        dashFacing = Mathf.Sign(_model.localScale.x);
        rb.useGravity = false;
    }
   #endregion

    //Model flip handler
    private void LateUpdate()
    {
        HandleModelFlip(moveInput.x);
    }

    private void HandleModelFlip(float moveX)
    {
        if (!_stateController.CanMove) return;

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
