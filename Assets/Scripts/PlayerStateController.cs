using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    

    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private Animator _animator;

    public PlayerMovement Movement => _movement;
    public PlayerCombat Combat => _combat;
    public Animator Animator => _animator;

    private BasePlayerState currentState;

    public GroundState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }

    [SerializeField]public bool CanMove { get; private set; } = true;
    public bool CanJump { get; private set; } = true;
    public bool IsAirborne => _movement != null && !_movement.IsGrounded;

    
    private void Awake()
    {
        GroundedState = new GroundState(this);
        AirborneState = new AirborneState(this);
        SwitchState(GroundedState);

    }


    // private void Start()
    // {
    // }

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
       public void SetMovePermission(bool canMove) => CanMove = canMove;
    public void SetJumpPermission(bool canJump) => CanJump = canJump;

    
}

