using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [Header("Distance Setting")]
    public float detectRange = 15f;
    public float chaseRange = 10f;
    public float attackRange = 2.5f;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Attack")]
    public List<AttackData> attacks;
    public float attackCooldown = 2f;
    private bool canAttack = true;

    private HealthComponent health;

    private void Start()
    {
        health = GetComponent<HealthComponent>();
        FindTarget();

        if (health != null)
        {
            health.OnDamaged += () => anim.SetTrigger("damage");
            health.OnStun += () => anim.SetBool("stun", true);
            health.OnDie += () => anim.SetBool("dead", true);
        }
    }

    private void Update()
    {
        if (health != null && health.IsStunned()) return;

        if (health != null && health.currentHealth <= 0) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectRange)
        {
            if (distance > attackRange)
            {
                ChaseTarget();
            }
            else
            {
                TryAttack(distance);
            }
        }
    }

    void FindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null)
            target = obj.transform;
    }

    void ChaseTarget()
    {
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        anim.SetBool("walk", true);

        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void TryAttack(float distance)
    {
        if (!canAttack) return;

        anim.SetBool("walk", false);

        AttackData selected = GetRandomAttackByDistance(distance);

        if (selected != null)
        {
            StartCoroutine(DoAttack(selected));
        }
    }

    AttackData GetRandomAttackByDistance(float distance)
    {
        var validAttacks = new System.Collections.Generic.List<AttackData>();

        foreach (var atk in attacks)
        {
            if (distance >= atk.minRange && distance <= atk.maxRange)
            {
                validAttacks.Add(atk);
            }
        }

        if (validAttacks.Count == 0) return null;

        int rand = Random.Range(0, validAttacks.Count);
        return validAttacks[rand];
    }

    IEnumerator DoAttack(AttackData attack)
    {
        canAttack = false;

        if (anim != null)
        {
            int index = attacks.FindIndex(a => a.attackName == attack.attackName);
            anim.SetInteger("skill", index + 1);
            Debug.Log("index : " + index);
        }

        yield return new WaitForSeconds(attack.delayBeforeHit);

        if (target != null)
        {
            var hp = target.GetComponent<HealthComponent>();
            if (hp != null)
            {
                hp.TakeDamage(attack.damage, attack.poiseDamage);
            }
        }

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }
}