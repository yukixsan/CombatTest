using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerLocomotionFX _locomotionFX;
    [SerializeField] private HealthComponent _health;

    public PlayerMovement Movement => _movement;
    public PlayerCombat Combat => _combat;
    public Animator Animator => _animator;
    public PlayerLocomotionFX LocomotionFX => _locomotionFX;
    public HealthComponent Health => _health;

    private BasePlayerState currentState;

    public GroundState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }
    public DamagedState DamagedState { get; private set; }
    public PlayerAirborneDamagedState AirborneDamagedState { get; private set; }
    public DeadState DeadState { get; private set; }

    [SerializeField]public bool CanMove { get; private set; } = true;
    public bool CanJump { get; private set; } = true;
    public bool IsAirborne => _movement != null && !_movement.IsGrounded;
    public bool IsDead => currentState == DeadState;
    public bool IsAlive => !IsDead;
    public bool CombatInputLocked { get; private set; }
    public bool CanAcceptCombatInput => IsAlive && !CombatInputLocked;
    public bool IsCrouching {get; private set; }
    public void SetCrouching(bool value) => IsCrouching = value;

    public bool CanFlip {get; private set; } = true;
    public void SetFlipPermission(bool canFlip) => CanFlip = canFlip;

    
    private void Awake()
    {
        GroundedState = new GroundState(this);
        AirborneState = new AirborneState(this);
        DamagedState = new DamagedState(this);
        AirborneDamagedState = new PlayerAirborneDamagedState(this);
        DeadState = new DeadState(this);
        SwitchState(GroundedState);

    }

    private void Update()
    {
        currentState.OnUpdate();
    }

    public void SwitchState(BasePlayerState newState)
    {
        if (currentState == newState) return;
        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }
    public void TriggerDamaged(HitboxPayload payload)
    {
         if (currentState == DeadState) return; // dead stays dead
    
        if (!Movement.IsGrounded || currentState == AirborneState || currentState == AirborneDamagedState)
        {
            if (currentState == AirborneDamagedState)
            {
                AirborneDamagedState.Reset(payload);
                return;
            }
            AirborneDamagedState.SetPendingKnockback(payload);
            SwitchState(AirborneDamagedState);
            return;
        }

        if (currentState == DamagedState)
        {
            DamagedState.Reset(payload);
        }
        else
        {
            DamagedState.SetPendingKnockback(payload);
            SwitchState(DamagedState);
        }
    }
    public void TriggerDeath()
    {
        if (IsDead) return;

        LockCombatInput();
        _combat?.ResetAttack();
        _combat?.HandleDeath();
        SwitchState(DeadState);
    }

    public void LockCombatInput()
    {
        CombatInputLocked = true;
    }

    public void ReleaseCombatInput()
    {
        CombatInputLocked = false;
    }
       public void SetMovePermission(bool canMove) => CanMove = canMove;
    public void SetJumpPermission(bool canJump) => CanJump = canJump;

    
}

