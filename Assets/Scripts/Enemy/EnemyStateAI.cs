using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateAI : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Stunned,
        Dead,
        Knockback
    }

    [System.Serializable]
    public class AttackData
    {
        public string attackName;
        public float damage = 10;
        public float poiseDamage = 5;

        public float minRange = 0f;
        public float maxRange = 3f;

        public float delayBeforeHit = 0.5f;
    }

    #region REFERENCES
    public Animator anim;
    public Rigidbody rb;
    public Transform target;
    private HealthComponent health;
    #endregion

    #region DETECTION
    public string targetTag = "Player";
    public float detectRange = 15f;
    public float chaseRange = 10f;
    public float attackRange = 2.5f;
    #endregion

    #region MOVEMENT
    public float moveSpeed = 3f;
    private Vector3 moveVelocity;
    #endregion

    #region ATTACK
    public List<AttackData> attacks;
    public float attackCooldown = 2f;

    private bool canAttack = true;
    private Coroutine attackRoutine;

    private float currentDamage;
    private float currentPoiseDamage;

    public float GetDamage() => currentDamage;
    public float GetPoiseDamage() => currentPoiseDamage;
    #endregion

    #region KNOCKBACK
    private bool isKnockedBack;
    private float knockbackTimer;
    #endregion

    #region GROUND
    public Transform groundCheckPoint;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    #endregion

    private State currentState;

    #region UNITY

    void Awake()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        health = GetComponent<HealthComponent>();
        FindTarget();

        if (health != null)
        {
            health.OnStun += OnStun;
            health.OnStunEnd += OnStunEnd;
            health.OnDie += OnDie;
        }
    }

    void Update()
    {
        UpdateGround();
        UpdateState();
        HandleState();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnStun -= OnStun;
            health.OnStunEnd -= OnStunEnd;
            health.OnDie -= OnDie;
        }
    }

    #endregion

    #region STATE CORE

    void UpdateState()
    {
        if (health == null) return;

        if (health.IsDie())
        {
            ChangeState(State.Dead);
            return;
        }

        if (health.IsStunned())
        {
            ChangeState(State.Stunned);
            return;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0f && isGrounded)
            {
                isKnockedBack = false;

                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            ChangeState(State.Knockback);
            return;
        }

        if (target == null)
        {
            FindTarget();
            ChangeState(State.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > detectRange) ChangeState(State.Idle);
        else if (dist > chaseRange) ChangeState(State.Idle);
        else if (dist > attackRange) ChangeState(State.Chase);
        else ChangeState(State.Attack);
    }

    void HandleState()
    {
        switch (currentState)
        {
            case State.Idle: Idle(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
            case State.Stunned: Stunned(); break;
            case State.Dead: Dead(); break;
            case State.Knockback: break;
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;

        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    void EnterState(State state)
    {
        if (state == State.Attack)
            TryAttack();
    }

    void ExitState(State state)
    {
        if (state == State.Attack)
            StopAttack();
    }

    #endregion

    #region STATES

    void Idle()
    {
        moveVelocity = Vector3.zero;
        SetAnim(walk: false);
    }

    void Chase()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;

        moveVelocity = new Vector3(dir.x * moveSpeed, 0, 0);

        Flip(dir.x);
        SetAnim(walk: true);
    }

    void Attack()
    {
        moveVelocity = Vector3.zero;
        SetAnim(walk: false);
    }

    void Stunned()
    {
        moveVelocity = Vector3.zero;
        SetAnim(stun: true);
    }

    void Dead()
    {
        moveVelocity = Vector3.zero;
        rb.excludeLayers = LayerMask.GetMask("Default");
        SetAnim(dead: true);
    }

    #endregion

    #region MOVEMENT

    void ApplyMovement()
    {
        if (isKnockedBack) return;

        rb.MovePosition(transform.position + moveVelocity * Time.fixedDeltaTime);
    }

    #endregion

    #region KNOCKBACK

    public void ApplyKnockback(Vector3 force, float duration)
    {
        isKnockedBack = true;
        knockbackTimer = duration;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        SetFallAnim(true);
    }

    #endregion

    #region ATTACK

    void TryAttack()
    {
        if (!canAttack) return;

        float dist = Vector3.Distance(transform.position, target.position);
        AttackData atk = GetAttack(dist);

        if (atk != null)
            attackRoutine = StartCoroutine(DoAttack(atk));
    }

    IEnumerator DoAttack(AttackData atk)
    {
        canAttack = false;

        int index = attacks.IndexOf(atk);
        SetAnim(skill: index + 1);

        currentDamage = atk.damage;
        currentPoiseDamage = atk.poiseDamage;

        yield return new WaitForSeconds(atk.delayBeforeHit);

        if (currentState != State.Attack)
        {
            ResetAttack();
            yield break;
        }

        yield return new WaitForSeconds(attackCooldown);

        ResetAttack();
    }

    void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        ResetAttack();
    }

    void ResetAttack()
    {
        canAttack = true;
        SetAnim(skill: 0);
    }

    AttackData GetAttack(float dist)
    {
        List<AttackData> valid = new List<AttackData>();

        foreach (var atk in attacks)
        {
            if (dist >= atk.minRange && dist <= atk.maxRange)
                valid.Add(atk);
        }

        if (valid.Count == 0) return null;

        return valid[Random.Range(0, valid.Count)];
    }

    #endregion

    #region GROUND + FALL

    void UpdateGround()
    {
        if (groundCheckPoint == null) return;

        isGrounded = Physics.CheckSphere(
            groundCheckPoint.position,
            groundCheckDistance,
            groundLayer
        );

        bool isFalling = !isGrounded && !rb.isKinematic && rb.linearVelocity.y < -0.1f;

        SetFallAnim(isFalling);

        if (isGrounded)
        {
            SetFallAnim(false);
        }
    }

    #endregion

    #region HELPERS

    void Flip(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = dirX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void SetAnim(bool walk = false, bool stun = false, bool dead = false, int skill = -1)
    {
        if (anim == null) return;

        anim.SetBool("walk", walk);
        anim.SetBool("stun", stun);
        anim.SetBool("dead", dead);

        if (skill != -1)
            anim.SetInteger("skill", skill);
    }

    void SetFallAnim(bool value)
    {
        if (anim == null) return;
        anim.SetBool("fall", value);
    }

    #endregion

    #region EVENTS

    void OnStun()
    {
        ChangeState(State.Stunned);
    }

    void OnStunEnd()
    {
        SetAnim(stun: false);
    }

    void OnDie()
    {
        ChangeState(State.Dead);
    }

    void FindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null)
            target = obj.transform;
    }

    #endregion
}