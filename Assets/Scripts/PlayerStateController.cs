using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Attacking
    }

    [Header("References")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerAttackHandler _attackHandler;
    [SerializeField] private Animator _animator;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    private void Update()
    {
        // Update based on what attackHandler is doing
        if (_attackHandler.IsAttacking)
        {
            SetState(PlayerState.Attacking);
        }
        else if (!_movement.IsGrounded)
        {
            SetState(PlayerState.Jumping);
        }
        else if (_movement.IsMoving)
        {
            SetState(PlayerState.Moving);
        }
        else
        {
            SetState(PlayerState.Idle);
        }
    }

    public void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        // Optional: hook into Animator
        _animator.SetBool("isRunning", CurrentState == PlayerState.Moving);
        _animator.SetBool("isJumping", CurrentState == PlayerState.Jumping);
        _animator.SetBool("isAttacking", CurrentState == PlayerState.Attacking);
    }

    public bool CanMove => CurrentState != PlayerState.Attacking;
    public bool CanAttack => CurrentState != PlayerState.Attacking;
}

