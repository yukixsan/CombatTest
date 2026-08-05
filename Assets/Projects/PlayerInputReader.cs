using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Runtime.InteropServices;

public class PlayerInputReader : MonoBehaviour
{
    //[SerializeField] private PlayerInputActions inputActions;
    [SerializeField] private CommandBuffer commandBuffer;
    [SerializeField] private CommandInterpreter interpreter;
    
    private void Awake()
    {
        //inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
              var hub = PlayerInputHub.Instance;
                if (hub == null) Debug.LogError("[Reader] PlayerInputHub instance not found on enable!");

        PlayerInputHub.Instance.Direction.performed += OnDirectionPerformed;
        hub.Direction.canceled += OnDirectionPerformed;

        hub.Attack.performed += OnAttackPerformed;

        hub.Skill01.performed += OnSkill01Performed;
        hub.Skill02.performed += OnSkill02Performed;
        hub.Skill03.performed += OnSkill03Performed;
        hub.Skill04.performed += OnSkill04Performed;

        hub.Dash.performed += OnDashPerformed;
        Debug.Log($"[Reader] Dash subscribed, action enabled: {hub.Dash.enabled}");
    }

   
    private void OnDisable()
    {
         var hub = PlayerInputHub.Instance;
        if (hub == null) return;

        hub.Direction.performed -= OnDirectionPerformed;
        hub.Direction.canceled -= OnDirectionPerformed;
        hub.Attack.performed -= OnAttackPerformed;

        hub.Skill01.performed -= OnSkill01Performed;
        hub.Skill02.performed -= OnSkill02Performed;
        hub.Skill03.performed -= OnSkill03Performed;
        hub.Skill04.performed -= OnSkill04Performed;

        hub.Dash.performed -= OnDashPerformed;
    }
    private void OnSkill01Performed(InputAction.CallbackContext ctx) => commandBuffer.Enqueue(CommandType.Skill, 0);
    private void OnSkill02Performed(InputAction.CallbackContext ctx) => commandBuffer.Enqueue(CommandType.Skill, 1);
    private void OnSkill03Performed(InputAction.CallbackContext ctx) => commandBuffer.Enqueue(CommandType.Skill, 2);
    private void OnSkill04Performed(InputAction.CallbackContext ctx) => commandBuffer.Enqueue(CommandType.Skill, 3);
    private void OnDashPerformed(InputAction.CallbackContext ctx) => commandBuffer.Enqueue(CommandType.Dash);    
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
