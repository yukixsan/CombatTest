using UnityEngine;

public abstract class BasePlayerState 
{
    protected PlayerStateController controller;
    protected PlayerMovement movement;
    protected PlayerCombat combat;
    protected Animator animator;

    public BasePlayerState(PlayerStateController controller)
    {
        this.controller = controller;
        this.movement = controller.Movement;
        this.combat = controller.Combat;
        this.animator = controller.Animator;
    }

    public virtual void OnEnter()
    {

    }
    public virtual void OnExit() { }
    public virtual void OnUpdate() { }
}
