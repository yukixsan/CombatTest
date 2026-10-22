using System;
using UnityEngine;

public class AwakeningManager : MonoBehaviour
{
    public static AwakeningManager Instance { get; private set; }

    [SerializeField] private AwakeningData awakeningData;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    public bool IsAwakened { get; private set; }
    public float RemainingTime { get; private set; }
    public AwakeningData Data => awakeningData;

    public float DamageMultiplier => IsAwakened && awakeningData != null ? awakeningData.damageMultiplier : 1f;
    public float MoveSpeedMultiplier => IsAwakened && awakeningData != null ? awakeningData.moveSpeedMultiplier : 1f;

    public event Action OnAwakeningStarted;
    public event Action OnAwakeningEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void TriggerAwakening()
    {
        if (awakeningData == null)
        {
            Debug.LogWarning("[AwakeningManager] Cannot trigger awakening: awakeningData is not assigned");
            return;
        }

        IsAwakened = true;
        RemainingTime = awakeningData.duration;

        if (animator != null && awakeningData.animSpeedMultiplier > 0f)
            animator.speed = awakeningData.animSpeedMultiplier;

        if (playerMovement != null)
            playerMovement.SetMoveSpeedMultiplier(awakeningData.moveSpeedMultiplier);

        Debug.Log("[AwakeningManager] Awakening activated!");
        OnAwakeningStarted?.Invoke();
    }

    public void DeactivateAwakening()
    {
        if (!IsAwakened) return;

        IsAwakened = false;
        RemainingTime = 0f;

        if (animator != null)
            animator.speed = 1f;

        if (playerMovement != null)
            playerMovement.SetMoveSpeedMultiplier(1f);

        Debug.Log("[AwakeningManager] Awakening deactivated.");
        OnAwakeningEnded?.Invoke();
    }

    private void Update()
    {
        if (!IsAwakened) return;

        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0f)
        {
            DeactivateAwakening();
        }
    }
}

