using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using static AttackData;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttackHandler _attackHandler;

    [Header("Attack")]
    [SerializeField] private AttackData _neutralAttack;
    [SerializeField] private AttackData _upAttack;
    [SerializeField] private AttackData _downAttack;

    [Header("Skills")]
    [SerializeField] private PlayerSkillData _dash;
    [SerializeField] private PlayerSkillData _skillQ;
    [SerializeField] private PlayerSkillData _skillW;
    [SerializeField] private PlayerSkillData _skillE;

    private PlayerInputActions _input;

    [Header("Directional Input")]
    // Directional buffer
    private Vector2 _lastDirection;
    private float _lastDirectionTime;
    [SerializeField] private const float DirectionBufferTime = 0.2f;
    private void Awake()
    {
        _input = new PlayerInputActions();

        // Bind input events to handler methods
        _input.Gameplay.Attack.performed += _ => HandleAttack();
        _input.Gameplay.Dash.performed += _ => _attackHandler.TrySkill(_dash);
        _input.Gameplay.Skill01.performed += _ => _attackHandler.TrySkill(_skillQ);
        _input.Gameplay.Skill02.performed += _ => _attackHandler.TrySkill(_skillW);
        _input.Gameplay.Skill03.performed += _ => _attackHandler.TrySkill(_skillE);

        // Bind directional input
        _input.Gameplay.Direction.performed += ctx => {
            _lastDirection = ctx.ReadValue<Vector2>();
            _lastDirectionTime = Time.time;
        };
        _input.Gameplay.Direction.canceled += ctx => {
            _lastDirection = Vector2.zero;
        };
    }

    private void HandleAttack()
    {
        Vector2 dir = Vector2.zero;

        if (Time.time - _lastDirectionTime <= DirectionBufferTime)
            dir = _lastDirection;

        if (dir.y > 0.5f)
            _attackHandler.TryDirectionalAttack(_upAttack);
        else if (dir.y < -0.5f)
            _attackHandler.TryDirectionalAttack(_downAttack);
        else
            _attackHandler.TryAttack();
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    private void OnDisable()
    {
        _input.Disable();
    }

}


