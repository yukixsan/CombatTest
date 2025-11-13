using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private CommandBuffer commandBuffer;
    [SerializeField] private CommandInterpreter interpreter;
    [SerializeField] private PlayerStateController _stateController;

    [Header("Attack library (fill in inspector)")]
    [SerializeField] private List<AttackData> groundAttacks = new();
    [SerializeField] private List<AttackData> airAttacks = new();
    [SerializeField] private List<PlayerSkillData> groundSkills = new();
    [SerializeField] private List<PlayerSkillData> airSkills = new();   


    [Header("Runtime")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHitbox _hitbox;
    [SerializeField] private Transform _model;

    [Header("Combo timing (used for reset)")]
    [SerializeField] private float comboResetTime = 0.5f;
      // combo progression (now owned here)
    private int currentComboIndex = 0;     // last started combo number (1..N), 0 = none
    private float lastAttackStartTime = -999f;
    // runtime state
    [SerializeField] private bool _isAttacking;
    public bool isAttacking
    {
        get => _isAttacking;
        private set => _isAttacking = value;
    }
    private bool cancelWindowOpen = false;
    [SerializeField] private AttackData queuedAttack = null;
    [SerializeField] private AttackData currentAttack = null;
    [SerializeField] private PlayerSkillData currentSkill = null;
    private PlayerSkillData queuedSkill = null;


    void Start()
    {
        _hitbox.enabled = false;
    }
    private void FixedUpdate()
    {
        bool allowBuffer = !isAttacking || cancelWindowOpen;
        commandBuffer.Process(interpreter, allowBuffer);
    }
    public void ExecuteAttack(Vector2 direction)
    {
        DirectionVariant variant = DirectionToVariant(direction);

        // decide next combo index *now* (so spamming won't change it unexpectedly)
        // reset combo if too slow
        if (Time.time - lastAttackStartTime > comboResetTime)
            currentComboIndex = 0;

        int nextCombo = currentComboIndex + 1;
        //if (nextCombo > attacks.Count) nextCombo = 1; // wrap or clamp as desired

        // find the matching AttackData for this combo slot & direction
        var data = FindMatchingAttack(nextCombo, variant);
        if (data == null)
        {
            Debug.Log($"[Combat] No matching AttackData for combo {nextCombo} dir {variant}");
            return;
        }
         // If not attacking, start immediately and record comboIndex/time
        if (!isAttacking)
        {
            StartAttack(data);
            currentComboIndex = nextCombo;
            lastAttackStartTime = Time.time;
            return;
        }

        // Already attacking: if currently in cancel window, interrupt and start new
        if (cancelWindowOpen)
        {
            Debug.Log($"[Combat] Interrupting current for immediate start of {data.Name}");
            StartAttack(data);
            currentComboIndex = nextCombo;
            lastAttackStartTime = Time.time;
            return;
        }
        // Only allow one queued attack; ignore duplicates to avoid spam overwriting the queued slot.
        if (queuedAttack != null)
        {
            // same attack already queued? ignore. This prevents LIFO overwriting.
            if (queuedAttack == data)
            {
                // already queued; ignore
                Debug.Log($"[Combat] Duplicate queued attack ignored: {data.Name}");
                return;
            }

            // If there is already a different queued attack, keep the earlier one (FIFO).
            Debug.Log($"[Combat] Attack already queued ({queuedAttack.Name}), ignoring new queued {data.Name}");
            return;
        }

        queuedAttack = data;
        Debug.Log($"[Combat] Queued attack {data.Name} (combo {nextCombo}, dir {variant})");
    }

    private void StartAttack(AttackData data)
    {
        Debug.Log($"[Combat] Start Attack '{data.Name}' (combo {data.comboIndex}, dir {data.directionVariant})");
        currentAttack = data;
        isAttacking = true;
        cancelWindowOpen = false;
        queuedAttack = null;

        if (animator != null && data.animationClip != null)
        {
            try { animator.Play(data.animationClip.name); }
            catch { Debug.LogWarning($"Animation state {data.animationClip.name} not found"); }
        }
    }

    //Animation events
     public void OnWindupStart()
    {
        cancelWindowOpen = (currentAttack != null && currentAttack.canBeCancelledWindup) ||
                            (currentSkill != null && currentSkill.canBeCancelledWindup);
        Debug.Log($"[Combat] Phase: WINDUP - cancel={cancelWindowOpen}");
        _stateController.SetMovePermission(false);
        _stateController.SetJumpPermission(false);
    }

    public void OnActiveStart()
    {
        if (currentAttack == null && currentSkill == null) return;

        cancelWindowOpen = (currentAttack != null && currentAttack.canBeCancelledActive) ||
                                    (currentSkill != null && currentSkill.canBeCancelledActive);
        Debug.Log($"[Combat] Phase: ACTIVE - cancel={cancelWindowOpen}");
            // handle attack hitbox
        if (currentAttack != null)
        {
            _hitbox.enabled = true;
            var payload = new HitboxPayload(
                currentAttack.damage,
                currentAttack.knockbackForce,
                currentAttack.launchForce,
                currentAttack.launchDir,
                currentAttack.hitstunDuration,
                transform,
                currentAttack.VFXindex
            );
            _hitbox.SetPayload(payload);
        }

        // handle skill hitbox (or movement / effect)
        if (currentSkill != null)
        {
            _hitbox.enabled = true;
            var payload = new HitboxPayload(
                currentSkill.damage,
                currentSkill.knockbackForce,
                currentSkill.launchForce,
                currentSkill.launchDir,
                currentSkill.hitstunDuration,
                transform,
                currentSkill.VFXindex
            );
            _hitbox.SetPayload(payload);
        }
    }

    public void OnRecoveryStart()
    {
        if (currentAttack == null&& currentSkill == null) return;

        _hitbox.enabled = false;
        _hitbox.ClearPayload();

        _stateController.SetMovePermission(true);
        _stateController.SetJumpPermission(true);

        cancelWindowOpen = (currentAttack != null && currentAttack.canBeCancelledRecovery) ||
                            (currentSkill != null && currentSkill.canBeCancelledRecovery);
        Debug.Log($"[Combat] Phase: RECOVERY - cancel={cancelWindowOpen}");
        TryQueuedAttack();
    }
    public void OnAttackEnd()
    {
        //Debug.Log($"[Combat] End Attack '{currentAttack.Name}'");
        
        isAttacking = false;
        cancelWindowOpen = false;
        currentAttack = null;

        // If queued attack is present (queued during earlier phases), start it now
        if (queuedAttack != null)
        {
            var next = queuedAttack;
            queuedAttack = null;
            StartAttack(next);

            // advance combo index and timestamp: we assume that queued attack is the "next" combo
            currentComboIndex++;
            //if (currentComboIndex > attacks.Count) currentComboIndex = 1;
            lastAttackStartTime = Time.time;
            return;
        }

        //animator.Play("Idle");
    }
    
     private void TryQueuedAttack()
    {
        if (!cancelWindowOpen) return;
        if (queuedAttack == null) return;

        var next = queuedAttack;
        queuedAttack = null;
        Debug.Log($"[Combat] Consuming queued attack {next.Name} during cancel window");
        StartAttack(next);

        // advance combo index/timestamp as attack starts
        currentComboIndex++;
        //if (currentComboIndex > attacks.Count) currentComboIndex = 1;
        lastAttackStartTime = Time.time;
    }
    public void ExecuteSkill(int skillIndex)
    {
        var data = FindMatchingSkill(skillIndex);
        if (data == null)
        {
            print("failed skill"); 
            return;
        }

        if (!isAttacking)
        {
            StartSkill(data);
            return;
        }

        if (cancelWindowOpen)
        {
            StartSkill(data);
            return;
        }
        
        if (queuedSkill != null)
        {
            Debug.Log($"[Combat] Skill already queued ({queuedSkill.Name}), ignoring {data.Name}");
            return;
        }

        queuedSkill = data;
        Debug.Log($"[Combat] Queued skill {data.Name}");
    
    }
    private void StartSkill(PlayerSkillData data)
    {
        Debug.Log($"[Combat] Start Attack '{data.Name}')");
        currentSkill = data;
        isAttacking = true;
        cancelWindowOpen = false;
        queuedAttack = null;

        if (data.skillPrefab != null)
        {
            Vector3 offset = data.spawnOffset;
            offset.x *= Mathf.Sign(_model.localScale.x);

            GameObject obj = Instantiate(data.skillPrefab,
                transform.position + offset,
                transform.rotation);

            SkillObject skill = obj.GetComponent<SkillObject>();
            if (skill != null)
                skill.Initialize(data, _model);

            if (data.attachToPlayer)
                obj.transform.SetParent(_model, worldPositionStays: true);
        }
        if (animator != null && data.animationClip != null)
        {
            try { animator.Play(data.animationClip.name); }
            catch { Debug.LogWarning($"Animation state {data.animationClip.name} not found"); }
        }
    }
    public void ExecuteDash()
    {
        Debug.Log("[Combat] Dash executed");
    }
    private AttackData FindMatchingAttack(int comboIndex, DirectionVariant variant)
    {
        bool isAirborne = _stateController != null && _stateController.IsAirborne;
        var attacks = isAirborne ? airAttacks : groundAttacks;
        // Step 1: Try exact match
        foreach (var a in attacks)
        {
            if (a == null) continue;

            if (a.comboIndex == comboIndex && a.directionVariant == variant)
            {
                if (a.Airborne == true)
                    a.airLimit++;
                if (a.airLimit > 1)
                    return null;
                return a;
            }

        }
        // Step 1.5: Direction-only fallback (same variant, ignore comboIndex)
        foreach (var a in attacks)
        {
            if (a == null) continue;
            if (a.directionVariant == variant && a.comboIndex == 0) // or ignore comboIndex entirely
            {
                if (a.Airborne == true)
                    a.airLimit++;
                if (a.airLimit > 1)
                    continue;
                return a;
            }
        }
        // Step 2: Fallback to neutral
        foreach (var a in attacks)
        {
            if (a == null) continue;
            if (a.comboIndex == comboIndex && a.directionVariant == DirectionVariant.Neutral)
            {
                if (a.Airborne == true)
                    a.airLimit++;
                if (a.airLimit > 1)
                    continue;
                return a;

            }
        }

        // Step 3: Optional fallback (e.g., if air attack missing, fall back to ground)
        if (isAirborne && groundAttacks.Count > 0)
        {
            foreach (var a in groundAttacks)
            {
                if (a == null) continue;
                if (a.comboIndex == comboIndex && a.directionVariant == variant)
                    return a;
            }
        }

        return null;
    }
    private PlayerSkillData FindMatchingSkill(int index)
    {
        bool isAirborne = _stateController != null && _stateController.IsAirborne;
        var list = isAirborne ? airSkills : groundSkills;

        if (index < 0 || index >= list.Count)
            return null;

        var skill = list[index];
        if (skill == null)
            return null;
        
        // Optional safety check
        if (isAirborne && !skill.Airborne) return null;
        if (!isAirborne && skill.Airborne) return null;

        return skill;
    }
    
    
     private static DirectionVariant DirectionToVariant(Vector2 dir)
    {
        if (dir.y > 0.5f) return DirectionVariant.Up;
        if (dir.y < -0.5f) return DirectionVariant.Down;
        return DirectionVariant.Neutral;
    }
    private string DirectionToString(Vector2 dir)
    {
        if (dir.y > 0.5f) return "↑";
        if (dir.y < -0.5f) return "↓";
        if (dir.x > 0.5f) return "→";
        if (dir.x < -0.5f) return "←";
        return "(neutral)";
    }
    public void ResetAttack()
    {
        foreach(var a in airAttacks)
        {
            a.airLimit = 0;
        }
        currentAttack = null;
        currentSkill = null;
        cancelWindowOpen = false;
        isAttacking = false;
        _hitbox.ClearPayload();

    
    }
}
