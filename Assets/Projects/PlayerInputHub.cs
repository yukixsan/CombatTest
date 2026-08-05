using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single owner of the Player's InputActionAsset lookups. Wraps PlayerInput
/// so PlayerMovement / PlayerInputReader / (later) PauseManager all share
/// ONE enabled action map state instead of each maintaining their own
/// `new PlayerInputActions()` instance (which was the root cause of the
/// pause-map desync bugs — three separate instances meant disabling one
/// script's Gameplay map never affected the others).
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHub : MonoBehaviour
{
    public static PlayerInputHub Instance { get; private set; }

    [SerializeField] private PlayerInput _playerInput;
    public bool IsGameplayMap => CurrentMapName == "Gameplay";


    // Cached action references — resolved once in Awake via FindAction,
    // mirroring what the generated PlayerInputActions wrapper did internally.
    public InputAction Move { get; private set; }
    public InputAction Jump { get; private set; }
    public InputAction Direction { get; private set; }
    public InputAction Attack { get; private set; }
    public InputAction Dash { get; private set; }
    public InputAction Skill01 { get; private set; }
    public InputAction Skill02 { get; private set; }
    public InputAction Skill03 { get; private set; }
    public InputAction Skill04 { get; private set; }
    public InputAction Crouch { get; private set; }

    public InputAction UICancel { get; private set; }
    // public InputAction Pause { get; private set; }


    public string CurrentMapName => _playerInput.currentActionMap != null
        ? _playerInput.currentActionMap.name
        : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _playerInput = GetComponent<PlayerInput>();

        var asset = _playerInput.actions;

        Move = asset.FindAction("Gameplay/Move");
        Jump = asset.FindAction("Gameplay/Jump");
        Direction = asset.FindAction("Gameplay/Direction");
        Attack = asset.FindAction("Gameplay/Attack");
        Dash = asset.FindAction("Gameplay/Dash");
        Skill01 = asset.FindAction("Gameplay/Skill01");
        Skill02 = asset.FindAction("Gameplay/Skill02");
        Skill03 = asset.FindAction("Gameplay/Skill03");
        Skill04 = asset.FindAction("Gameplay/Skill04");
        Crouch = asset.FindAction("Gameplay/Crouch");

        UICancel = asset.FindAction("UI/Cancel");
        // Pause = asset.FindAction("Gameplay/Pause");

    }

    private void OnEnable()
    {
        // UI/Cancel must always be listenable regardless of which map is
        // "current" — PlayerInput only auto-enables the current map's actions,
        // so Cancel needs an explicit manual Enable() to stay live for the
        // pause toggle later. Harmless no-op cost for actions not being read
        // by gameplay this step.
        UICancel.Enable();
    }

    private void OnDisable()
    {
        UICancel.Disable();
    }

    /// <summary>Switches the active action map. Actions not in the current
    /// map are NOT automatically disabled by PlayerInput — UICancel is kept
    /// alive manually above for that reason.</summary>
    public void SwitchToGameplay()
    {
        _playerInput.SwitchCurrentActionMap("Gameplay");
        UICancel.Enable();
    }
    public void SwitchToUI() => _playerInput.SwitchCurrentActionMap("UI");

}