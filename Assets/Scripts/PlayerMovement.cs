using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float _fallMult;
    // [SerializeField] private float moveStopDebounce = 0.08f; // seconds of near-zero input before IsMoving reports false
    // private float _lastMovingTime;
    private float tempFallMult;
    [SerializeField] private Transform _model;
    private Vector3 externalVelocity;

    [Header("Knockback")]
    private bool isKnockedBack;
    private Vector3 knockbackVelocity;
    public bool IsKnockedBack => isKnockedBack;

    [Header("Dash Settings")]
    private bool isDashing;
    private float dashTimer;
    
    private float _activeDashSpeed;
    private float _activeDashDuration;
    private float dashFacing;
    private Vector2 _dashDirection = Vector2.right;


    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("One way collision checks")]
    [SerializeField] private OneWayPlatformHandler oneWayHandler;

    // Components
    private Rigidbody rb;
    //private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private float? _automatedMoveInputX;
    [SerializeField] private Vector3 _streetForward = Vector3.right;
    public Vector3 StreetForward => _streetForward;
    [SerializeField]private bool _isGrounded;
    public bool IsMoving { get; private set; }
    public float HorizontalInput => moveInput.x;
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public void SetMoveSpeedMultiplier(float mult) => MoveSpeedMultiplier = Mathf.Max(0f, mult);
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
    [SerializeField] private float _lockedDepth;
    public bool IsGrounded
    {
        get { return _isGrounded; } set {  _isGrounded = value; }
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        bool isZStreet = Mathf.Abs(_streetForward.z) > 0.5f;
        _lockedDepth = isZStreet ? transform.position.x : transform.position.z;

        RigidbodyConstraints posFreeze = isZStreet 
            ? RigidbodyConstraints.FreezePositionX 
            : RigidbodyConstraints.FreezePositionZ;

        rb.constraints = posFreeze | RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
         var hub = PlayerInputHub.Instance;

        hub.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        hub.Move.canceled += ctx => moveInput = Vector2.zero;

        hub.Jump.performed += OnJumpPerformed;

        hub.Crouch.performed += ctx => _stateController.SetCrouching(true);
        hub.Crouch.canceled += ctx => _stateController.SetCrouching(false);
    }

    private void OnDisable()
    {
         var hub = PlayerInputHub.Instance;
        if (hub == null) return; // hub may already be destroyed on teardown

        hub.Jump.performed -= OnJumpPerformed;
        hub.Crouch.performed -= ctx => _stateController.SetCrouching(true);
        hub.Crouch.canceled -= ctx => _stateController.SetCrouching(false);
    }

    private void FixedUpdate()
    {
        //Ground check
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        Vector3 velocity = rb.linearVelocity;
        
        // 1️⃣ KNOCKBACK CHECK
        if(isKnockedBack)
        {
            rb.linearVelocity = knockbackVelocity;
            return; // Knockback overrides everything for 1 frame
        }

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
            rb.useGravity = false;
            Vector3 dashHorizontal = _streetForward * (_dashDirection.x * _activeDashSpeed);
            externalVelocity = new Vector3(dashHorizontal.x, _dashDirection.y * _activeDashSpeed, dashHorizontal.z);
            velocity = externalVelocity;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.useGravity = true;
                externalVelocity = Vector3.zero;
                velocity = Vector3.zero;
            }
        }

        // 4️⃣ BASE MOVEMENT (INPUT LAYER)
        float baseX = 0f;

        bool allowBaseMovement = _stateController.CanMove;
        float inputX = _automatedMoveInputX ?? moveInput.x;
        bool hasMovePermission = allowBaseMovement || _automatedMoveInputX.HasValue;

        // Disable input movement if - movement not allowed - OR currently dashing
        if (hasMovePermission && !isDashing)
        {
            baseX = inputX * moveSpeed * MoveSpeedMultiplier;
        }

        Vector3 moveHorizontal = _streetForward * baseX;
        velocity.x = moveHorizontal.x + externalVelocity.x;
        velocity.z = moveHorizontal.z + externalVelocity.z;

        // Enforce 2.5D depth lock on linear velocity and anchor position
        Vector3 curPos = rb.position;
        if (Mathf.Abs(_streetForward.z) > 0.5f)
        {
            velocity.x = 0f; // On Z street (highway), depth is X
            curPos.x = _lockedDepth;
        }
        else
        {
            velocity.z = 0f; // On X street (building), depth is Z
            curPos.z = _lockedDepth;
        }

        rb.position = curPos;
        rb.linearVelocity = velocity;

        IsMoving = Mathf.Abs(baseX) > 0.1f;

        // 5️⃣ JUMP
        if (_jumpPressed && _isGrounded && _stateController.CanJump)
        {
            velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;

            _stateController.LocomotionFX.PlayJumpDust(groundCheck.position);
        }
        _jumpPressed = false;

        // 6️⃣ FALL MULTIPLIER
        if (!_isGrounded && rb.linearVelocity.y < 0 && !isDashing)
        {
            rb.AddForce(Vector3.down * _fallMult, ForceMode.Acceleration);
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (_stateController.IsCrouching && oneWayHandler != null)
        {
            oneWayHandler.DropThrough();
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, -10, 0f);

            return;
        }
        _jumpPressed = true;
    }

    private void LaunchVelocity(Vector3 dir, float strength)
    {
        float facing = Mathf.Sign(_model.localScale.x); 

        // dir.x is forward/back along the active street
        Vector3 horizontalDir = _streetForward * (dir.x * facing);
        Vector3 finalDir = new Vector3(horizontalDir.x, dir.y, horizontalDir.z);

        pendingLaunchVelocity += finalDir.normalized * strength;
        hasPendingVelocity = true;
    }

    private void LaunchForce(Vector3 dir, float strength)
    {
        float facing = Mathf.Sign(_model.localScale.x);
        Vector3 horizontalDir = _streetForward * (dir.x * facing);
        Vector3 finalDir = new Vector3(horizontalDir.x, dir.y, horizontalDir.z);
        rb.AddForce(finalDir.normalized * strength, ForceMode.VelocityChange);
    }

    public void SetKnockbackVelocity(Vector3 velocity)
    {
        isKnockedBack = true;
        knockbackVelocity = velocity;
    }

    public void ClearKnockback()
    {
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
    }

    public void SetStreetTrack(Vector3 forwardDirection, RigidbodyConstraints constraints, Quaternion? targetRotation = null)
    {
        _streetForward = forwardDirection.sqrMagnitude > 0.001f ? forwardDirection.normalized : Vector3.right;
        rb.constraints = constraints;
        if (targetRotation.HasValue)
        {
            transform.rotation = targetRotation.Value;
            rb.rotation = targetRotation.Value;
        }

        bool isZStreet = Mathf.Abs(_streetForward.z) > 0.5f;
        _lockedDepth = isZStreet ? transform.position.x : transform.position.z;
    }

    public void SetStreetForward(Vector3 forwardDirection, Quaternion? targetRotation = null)
    {
        _streetForward = forwardDirection.sqrMagnitude > 0.001f ? forwardDirection.normalized : Vector3.right;
        if (targetRotation.HasValue)
        {
            transform.rotation = targetRotation.Value;
            rb.rotation = targetRotation.Value;
        }
    }

    public void SetAutomatedMoveInput(float? inputX)
    {
        _automatedMoveInputX = inputX;
    }

    public void TeleportTo(Vector3 worldPosition)
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = worldPosition;
        transform.position = worldPosition;
        bool isZStreet = Mathf.Abs(_streetForward.z) > 0.5f;
        _lockedDepth = isZStreet ? worldPosition.x : worldPosition.z;
        hasPendingVelocity = false;
        pendingLaunchVelocity = Vector3.zero;
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
        isDashing = false;
        externalVelocity = Vector3.zero;
        _jumpPressed = false;
    }

    #region Animation Event Presets 
    public void LaunchUp(float strength) => LaunchVelocity(Vector3.up, strength);
    public void LaunchForward(float strength) => LaunchVelocity(Vector3.right, strength);
    public void LaunchBack(float strength) => LaunchVelocity(Vector3.left, strength);
    public void LaunchDown(float strength) => LaunchVelocity(Vector3.down, strength);

    public void ForceUp(float strength) => LaunchForce(Vector3.up, strength);
    public void ForceForward(float strength) => LaunchForce(Vector3.right, strength);
    public void ForceBack(float strength) => LaunchForce(Vector3.left, strength);
    public void ForceDown(float strength) => LaunchForce(Vector3.down, strength);
    #endregion

#region Dash Presets
    public void setDashSpeed(float speed) => _activeDashSpeed = speed;
    public void setDashDuration(float duration) => _activeDashDuration = duration;
    
    public void ForceDashForward()
    {
        float facing = Mathf.Sign(_model.localScale.x);
        ForceDirectionalDash(new Vector2(facing, 0f));
    }

    public void ForceDirectionalDash(Vector2 direction)
    {
        ForceDirectionalDash(direction, _activeDashSpeed, _activeDashDuration);
    }

    public void ForceDirectionalDash(Vector2 direction, float speed, float duration)
    {
        if (isDashing) return; // Prevent overlapping dashes

        _activeDashSpeed = speed;
        _activeDashDuration = duration;
        isDashing = true;
        dashTimer = duration;
        _dashDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : new Vector2(Mathf.Sign(_model.localScale.x), 0f);
        rb.useGravity = false;
    }
   #endregion


    //Model flip handler
    private void LateUpdate()
    {
        float flipInput = _automatedMoveInputX ?? moveInput.x;
        HandleModelFlip(flipInput);
    }

    private void HandleModelFlip(float moveX)
    {
        if (!_stateController.CanFlip) return;

        if (moveX > 0)
        {
            _model.localScale = new Vector3(1, 1, 1);

        }
        else if (moveX < 0)
        {
            _model.localScale = new Vector3(-1, 1, 1);
        }
    }

    //Fall mult manipulation
    public void SetFallMult(float speed)
    {
        tempFallMult = _fallMult;
        _fallMult = speed;
    }
    public void ResetFallMult()
    {
        _fallMult = tempFallMult;
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
