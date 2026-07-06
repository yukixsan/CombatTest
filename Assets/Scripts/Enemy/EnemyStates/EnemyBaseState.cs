using UnityEngine;

public abstract class EnemyBaseState
{
    protected EnemyStateController controller;
    protected EnemyMovement movement;
    protected Rigidbody rb;
    protected Animator anim;

    public EnemyBaseState(EnemyStateController controller)
    {
        this.controller = controller;
        this.movement = controller.Movement;
        this.rb = controller.Rb;
        this.anim = controller.Anim;
    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
}
