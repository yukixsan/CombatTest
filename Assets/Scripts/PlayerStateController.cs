using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private Animator _animator;
    [SerializeField] private HealthComponent _health;

    public PlayerMovement Movement => _movement;
    public PlayerCombat Combat => _combat;
    public Animator Animator => _animator;
    public HealthComponent Health => _health;

    private BasePlayerState currentState;

    public GroundState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }
    public DamagedState DamagedState { get; private set; }
    public DeadState DeadState { get; private set; }

    [SerializeField]public bool CanMove { get; private set; } = true;
    public bool CanJump { get; private set; } = true;
    public bool IsAirborne => _movement != null && !_movement.IsGrounded;
    public bool CanFlip {get; private set; } = true;
    public void SetFlipPermission(bool canFlip) => CanFlip = canFlip;

    
    private void Awake()
    {
        GroundedState = new GroundState(this);
        AirborneState = new AirborneState(this);
        DamagedState = new DamagedState(this);
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
    public void TriggerDamaged()
    {
         if (currentState == DeadState) return; // dead stays dead
 
        if (currentState == DamagedState)
        {
            // Already damaged — reset the stun timer and replay animation
            DamagedState.Reset();
        }
        else
        {
            SwitchState(DamagedState);
        }
    }
    public void TriggerDeath()
    {
        SwitchState(DeadState); 
    }
       public void SetMovePermission(bool canMove) => CanMove = canMove;
    public void SetJumpPermission(bool canJump) => CanJump = canJump;

    
}

