using UnityEngine;

public class EnemyStateController : MonoBehaviour
{
      [Header("References")]
    [SerializeField] private EnemyMovement _movement;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Animator _anim;
    [SerializeField] private HealthComponent _health;
    [SerializeField] private EnemyHitBox _hitbox;

    public EnemyMovement Movement => _movement;
    public Rigidbody Rb => _rb;
    public Animator Anim => _anim;
    public HealthComponent Health => _health;
    public EnemyHitBox Hitbox => _hitbox;

    [Header("Detection")]
    public Transform target;
    public string targetTag = "Player";
    public float detectRange = 15f;
    public float chaseRange = 10f;
    public float stopDistance = 2f;

    [Header("State Tuning")]
    public float idleToChaseDelay = 2f;
    public float damagedDuration = 0.8f;
    public float attackDuration = 0.5f;
    public float attackCooldown = 1.5f;
    public bool canAttack = true;
    public float attackCooldownTimer = 0f;
    [Header("Attack Data (temporary, until EnemyAttackData exists)")]
public float attackDamage = 10f;
public float attackArmor = 0f;
public float attackKnockbackForce = 5f;
public float attackLaunchForce = 0f;
public int attackLaunchDir = 1;
public float attackHitstopDuration = 0.05f;
public int attackVFXIndex = 0;

    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyDamagedState DamagedState { get; private set; }
    public EnemyAirborneState AirborneState { get; private set; }
    public EnemyAirborneDamagedState AirborneDamagedState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }

    private EnemyBaseState currentState;
    public bool IsDamaged => currentState == DamagedState;
    public bool IsAirborne => currentState == AirborneState;
    public bool IsAirborneDamaged => currentState == AirborneDamagedState;
    public bool IsAttacking => currentState == AttackState;

    private void Awake()
    {
        IdleState = new EnemyIdleState(this);
        ChaseState = new EnemyChaseState(this);
        DamagedState = new EnemyDamagedState(this);
        AirborneState = new EnemyAirborneState(this);
        AirborneDamagedState = new EnemyAirborneDamagedState(this);
        AttackState = new EnemyAttackState(this);
        SwitchState(IdleState);
    }

    private void Start()
    {
        Debug.Log("EnemyStateController: Start() called");
        FindTarget();
        SwitchState(IdleState);

        if (_health != null)
        {
            _health.OnDie += HandleDeath;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDie -= HandleDeath;
        }
    }

    private void Update()
    {
        _movement.UpdateGroundCheck();
        currentState.OnUpdate();
    }

    private void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }

    public void SwitchState(EnemyBaseState newState)
    {
        
        if(currentState == newState && newState != DamagedState) return;
        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    /// External entry point — called by EnemyHurtbox when a hit lands and the
    /// armor-interrupt check DID NOT break super armor (i.e. mini-stun, not full stun).
    public void TriggerDamaged(HitboxPayload payload)
    {
        if (!_movement.IsGrounded || IsAirborne || IsAirborneDamaged)
        {
            if (IsAirborneDamaged)
            {
                AirborneDamagedState.Reset(payload);
                return;
            }
            AirborneDamagedState.SetPendingKnockback(payload);
            SwitchState(AirborneDamagedState);
            return;
        }

        DamagedState.SetPendingKnockback(payload);
        SwitchState(DamagedState);
    }

   

    private void FindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null) target = obj.transform;
    }

    private void HandleDeath()
    {
        // Placeholder until DeadState is implemented in a later phase.
        _movement.StopMovement();
    }

  
}
