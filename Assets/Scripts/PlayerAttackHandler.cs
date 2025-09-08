using System.Collections;
using UnityEngine;


public class PlayerAttackHandler : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Animator _animator;
    [SerializeField] private CombatActionData _startingAction;
    [SerializeField] private PlayerHitbox _hitbox;
    [SerializeField] private Transform _model;

    [Header("Variables")]
    private Coroutine _currentActionRoutine;
    [SerializeField]private CombatActionData _currentAction;
    private bool _isAttacking;
    private bool _inWindup, _inActive, _inRecovery;
    [SerializeField] private PlayerStateController _stateController;

    public bool IsInRecovery => _inRecovery;
    public bool IsAttacking => _isAttacking;



    private void Start()
    {
        _currentAction = _startingAction;
                _hitbox.enabled = false;

    }

    public void TryStartAction(CombatActionData newAction)
    {
        // Cannot start new action if we're in the middle of uncancellable phases
        if (_isAttacking && !CanBeCancelledBy(newAction))
            return;

        // Start or cancel current action
        if (_currentActionRoutine != null)
            StopCoroutine(_currentActionRoutine);

        _currentActionRoutine = StartCoroutine(ExecuteAction(newAction));
    }

    private bool CanBeCancelledBy(CombatActionData newAction)
    {
        if (!newAction) return false;

        // If it's a skill, check its cancel rules
        if (newAction is PlayerSkillData)
        {
            if (_inWindup && newAction.canBeCancelledWindup) return true;
            if (_inActive && newAction.canBeCancelledActive) return true;
            if (_inRecovery && newAction.canBeCancelledRecovery) return true;
        }

        return false;
    }

    private IEnumerator ExecuteAction(CombatActionData action)
    {
        _isAttacking = true;
        _currentAction = action;
        _hitbox.enabled = false;


        //Windup
        _inWindup = true;
        _animator.Play(action.animationClip.name);
        yield return new WaitForSeconds(action.windup);
        _inWindup = false;

        //Active
        _inActive = true;
        if (action is PlayerSkillData skillData)
        {
            LaunchSkill(skillData);
        }
        else if (action is AttackData   attackData)
        {
            // normal attack hitbox enabling
            _hitbox.enabled = true;
            var payload = new HitboxPayload(
                attackData.damage,
                attackData.knockbackForce,
                attackData.launchForce,
                attackData.launchDir,
                attackData.hitstunDuration,
                transform

            ) ;      
            _hitbox.SetPayload(payload);
        
        }

        print($"[Attack] Active: {action.name}");
        yield return new WaitForSeconds(action.active);
        _inActive = false;
        _isAttacking = false;

        // Follow-up or reset
        _hitbox.enabled = false;
        _hitbox.ClearPayload();

        if (action is AttackData atkData)
        {
            if (atkData.absoluteRecovery)
            {
                _currentAction = _startingAction;
            }
            else if (atkData.nextCombo != null)
            {
                _currentAction = atkData.nextCombo;
            }
            else
            {
                _currentAction = _startingAction;
            }
        }
        else
        {

            _currentAction = _startingAction;
        }

        //Recovery
        _inRecovery = true;
        print($"[Attack] Recovery: {action.name}");
        yield return new WaitForSeconds(action.recovery);
        _inRecovery = false;
        //Reset
        _currentAction = _startingAction;

        _stateController.SetState(PlayerStateController.PlayerState.Idle);

        _animator.CrossFade("Idle", 0.1f);
        _currentActionRoutine = null;
        
    }
    public void TryAttack()
    {
        if (_currentAction is AttackData)
            TryStartAction(_currentAction);
    }

    public void TryDirectionalAttack(AttackData attack)
    {
        if (_currentAction is AttackData)
            _currentAction = attack;
            TryStartAction(attack);
    }

    public void TrySkill(PlayerSkillData skill)
    {
        TryStartAction(skill);
    }

    private void LaunchSkill(PlayerSkillData skillData)
    {
        if (skillData.skillPrefab == null) return;
        Vector3 offset = skillData.spawnOffset;
        offset.x  *= Mathf.Sign(_model.localScale.x);

        GameObject obj = Instantiate(skillData.skillPrefab,
            transform.position + offset,
            transform.rotation);

        SkillObject skill = obj.GetComponent<SkillObject>();
        if (skill != null)
            skill.Initialize(skillData, _model);

        if (skillData.attachToPlayer)
            obj.transform.SetParent(_model, worldPositionStays: true);
    }
}
