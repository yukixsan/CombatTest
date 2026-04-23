using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateAI : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public string attackName;

        [Header("Damage")]
        public float damage = 10;
        public float poiseDamage = 5;

        [Header("Range")]
        public float minRange = 0f;
        public float maxRange = 3f;

        [Header("Timing")]
        public float delayBeforeHit = 0.5f;
    }

    [Header("Animator")]
    public Animator anim;

    [Header("Target")]
    public string targetTag = "Player";
    private Transform target;

    [Header("Distance")]
    public float detectRange = 15f;
    public float chaseRange = 10f;
    public float attackRange = 2.5f;

    [Header("Movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField]private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private const float KNOCKBACK_LOCKOUT = 0.25f; 
    public float moveSpeed = 3f;

    [Header("Attack")]
    public List<AttackData> attacks;
    public float attackCooldown = 2f;

    private bool canAttack = true;
    private Coroutine attackRoutine;

    private HealthComponent health;

    private float currentDamage;
    private float currentPoiseDamage;
    public float GetDamage() => currentDamage;
    public float GetPoiseDamage() => currentPoiseDamage;

    private void Start()
    {
        health = GetComponent<HealthComponent>();
        FindTarget();

        if (health != null)
        {
            health.OnStun += HandleStun;
            health.OnStunEnd += HandleStunEnd;
            health.OnDie += HandleDie;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnStun -= HandleStun;
            health.OnStunEnd -= HandleStunEnd;
            health.OnDie -= HandleDie;
        }
    }

    void Update()
    {
        if (health == null) return;

        if (health.IsDie())
        {
            StopAllActions();
            return;
        }

        if (health.IsStunned())
        {
            StopAllActions();
            return;
        }

        //Knocback checks
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if(knockbackTimer <= 0f)
            
                isKnockedBack = false;
            return;
        }

        if (target == null)
        {
            FindTarget();
            return;
        }

        var targetHp = target.GetComponent<HealthComponent>();
        if (targetHp != null && targetHp.IsDie())
        {
            Idle();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectRange)
        {
            if (distance > chaseRange)
            {
                Idle();
            }
            else if (distance > attackRange)
            {
                ChaseTarget();
            }
            else
            {
                TryAttack(distance);
            }
        }
        else
        {
            Idle();
        }
    }

    void FindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null)
            target = obj.transform;
    }

    void Idle()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        if (anim != null)
        {
            anim.SetBool("walk", false);
            anim.SetInteger("skill", 0);
        }
    }

    void ChaseTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        
        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y,0);

        if (anim != null)
            anim.SetBool("walk", true);

        Vector3 scale = transform.localScale;
        scale.x = dir.x > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void TryAttack(float distance)
    {
        if (!canAttack) return;

        AttackData selected = GetRandomAttackByDistance(distance);

        if (selected != null)
        {
            attackRoutine = StartCoroutine(DoAttack(selected));
        }
    }

    AttackData GetRandomAttackByDistance(float distance)
    {
        List<AttackData> valid = new List<AttackData>();

        foreach (var atk in attacks)
        {
            if (distance >= atk.minRange && distance <= atk.maxRange)
                valid.Add(atk);
        }

        if (valid.Count == 0) return null;

        return valid[Random.Range(0, valid.Count)];
    }

    IEnumerator DoAttack(AttackData attack)
    {
        canAttack = false;

        if (anim != null)
        {
            int index = attacks.IndexOf(attack);
            anim.SetInteger("skill", index + 1);
            anim.SetBool("walk", false);
            currentDamage = attack.damage;
            currentPoiseDamage = attack.poiseDamage;
        }

        yield return new WaitForSeconds(attack.delayBeforeHit);

        if (health.IsStunned() || health.IsDie())
        {
            ResetAttack();
            yield break;
        }

        //if (target != null)
        //{
        //    var hp = target.GetComponent<HealthComponent>();
        //    if (hp != null && !hp.IsDie())
        //    {
        //        hp.TakeDamage(attack.damage, attack.poiseDamage);
        //    }
        //}

        yield return new WaitForSeconds(attackCooldown);

        ResetAttack();
    }

    void ResetAttack()
    {
        canAttack = true;

        if (anim != null)
        {
            anim.SetInteger("skill", 0);
        }
    }

    void StopAllActions()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        canAttack = true;

        if (anim != null)
        {
            anim.SetBool("walk", false);
            anim.SetInteger("skill", 0);
        }
    }

    void HandleStun()
    {
        StopAllActions();

        if (anim != null)
            anim.SetBool("stun", true);
    }

    void HandleStunEnd()
    {
        if (anim != null)
            anim.SetBool("stun", false);
    }

    void HandleDie()
    {
        StopAllActions();

        if (anim != null)
            anim.SetBool("dead", true);
    }

    //Handle knocback 
    public void ApplyKnocback(float lockDuration)
    {
        isKnockedBack = true;
        knockbackTimer = lockDuration;
    }
}