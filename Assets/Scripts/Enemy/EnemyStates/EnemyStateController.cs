using UnityEngine;

public class EnemyStateController : MonoBehaviour
{
      [Header("References")]
    [SerializeField] private EnemyMovement _movement;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Animator _anim;
    [SerializeField] private HealthComponent _health;

    public EnemyMovement Movement => _movement;
    public Rigidbody Rb => _rb;
    public Animator Anim => _anim;
    public HealthComponent Health => _health;
    [Header("Detection")]
    public Transform target;
    public string targetTag = "Player";
    public float detectRange = 15f;
    public float chaseRange = 10f;

    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyDamagedState DamagedState { get; private set; }

    private EnemyBaseState currentState;
    public bool IsDamaged => currentState == DamagedState;

    private void Awake()
    {
        IdleState = new EnemyIdleState(this);
        ChaseState = new EnemyChaseState(this);
        DamagedState = new EnemyDamagedState(this);
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
        if (currentState == newState) return;
        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }
    /// External entry point — called by EnemyHurtbox when a hit lands and the
    /// armor-interrupt check DID NOT break super armor (i.e. mini-stun, not full stun).
    /// 
    public void TriggerDamaged(HitboxPayload payload)
    {
        // Dead/future-Stunned guard placeholder — for now, Damaged always accepts.
        (DamagedState as EnemyDamagedState)?.SetPendingKnockback(payload);
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
