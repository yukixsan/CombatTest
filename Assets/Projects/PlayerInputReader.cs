using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private PlayerInputActions inputActions;
    [SerializeField] private CommandBuffer commandBuffer;
    [SerializeField] private CommandInterpreter interpreter;

    private void Awake()
    {
    inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        // Movement
        inputActions.Gameplay.Direction.performed += OnDirectionPerformed;
        inputActions.Gameplay.Direction.canceled += OnDirectionPerformed;

        //Attack direction
        // Attack
        inputActions.Gameplay.Attack.performed += OnAttackPerformed;

        // Skills
        inputActions.Gameplay.Skill01.performed += ctx => commandBuffer.Enqueue(CommandType.Skill, 0);
        inputActions.Gameplay.Skill02.performed += ctx => commandBuffer.Enqueue(CommandType.Skill, 1);
        inputActions.Gameplay.Skill03.performed += ctx => commandBuffer.Enqueue(CommandType.Skill, 2);


        // Dash (optional if you have one)
        // inputActions.Gameplay.Dash.performed += ctx => commandBuffer.Enqueue(CommandType.Dash);
    }

   
    private void OnDisable()
    {
        inputActions.Gameplay.Direction.performed -= OnDirectionPerformed;
        inputActions.Gameplay.Direction.canceled -= OnDirectionPerformed;
        inputActions.Gameplay.Attack.performed -= OnAttackPerformed;

        inputActions.Gameplay.Disable();
    }

    private void OnDirectionPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 dir = ctx.ReadValue<Vector2>();
        interpreter.UpdateDirection(dir);
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        commandBuffer.Enqueue(CommandType.Attack);
    }
}
