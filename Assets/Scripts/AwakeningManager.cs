using System;
using UnityEngine;

public class AwakeningManager : MonoBehaviour
{
    public static AwakeningManager Instance { get; private set; }

    [SerializeField] private AwakeningData awakeningData;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Resource Runtime")]
    [SerializeField] private float currentResourceTime;
    [SerializeField] private float remainingCooldown;

    public bool IsAwakened { get; private set; }
    public float CurrentResourceTime => currentResourceTime;
    public float RemainingTime => currentResourceTime; // Backward-compatible alias
    public float MaxResourceTime => awakeningData != null ? awakeningData.maxGauge * awakeningData.secondsPerGauge : 10f;
    public int CurrentGaugeCount => awakeningData != null && awakeningData.secondsPerGauge > 0f ? Mathf.FloorToInt(currentResourceTime / awakeningData.secondsPerGauge) : 0;
    public float RemainingCooldown => remainingCooldown;
    public AwakeningData Data => awakeningData;

    public float DamageMultiplier => IsAwakened && awakeningData != null ? awakeningData.damageMultiplier : 1f;
    public float MoveSpeedMultiplier => IsAwakened && awakeningData != null ? awakeningData.moveSpeedMultiplier : 1f;

    public event Action OnAwakeningStarted;
    public event Action OnAwakeningEnded;
    public event Action<float, float> OnGaugeChanged;
    public event Action<float> OnCooldownChanged;

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

    public void AddGauge(int amount = 1)
    {
        if (awakeningData == null || amount <= 0) return;

        float addTime = amount * awakeningData.secondsPerGauge;
        currentResourceTime = Mathf.Clamp(currentResourceTime + addTime, 0f, MaxResourceTime);
        Debug.Log($"[AwakeningManager] Added {amount} gauge (+{addTime:F1}s). Current: {currentResourceTime:F1}s / {MaxResourceTime:F1}s (approx {CurrentGaugeCount}/{awakeningData.maxGauge} gauge)");
        OnGaugeChanged?.Invoke(currentResourceTime, MaxResourceTime);
    }

    public bool CanActivate(out string reason)
    {
        if (awakeningData == null)
        {
            reason = "No AwakeningData assigned";
            return false;
        }

        if (remainingCooldown > 0f)
        {
            reason = $"On cooldown ({remainingCooldown:F1}s remaining)";
            return false;
        }

        if (currentResourceTime < awakeningData.minSecondsToActivate)
        {
            reason = $"Insufficient gauge ({currentResourceTime:F1}s < {awakeningData.minSecondsToActivate:F1}s required)";
            return false;
        }

        reason = null;
        return true;
    }

    public void TriggerAwakening()
    {
        if (awakeningData == null)
        {
            Debug.LogWarning("[AwakeningManager] Cannot trigger awakening: awakeningData is not assigned");
            return;
        }

        if (currentResourceTime <= 0f)
        {
            Debug.LogWarning("[AwakeningManager] Cannot trigger awakening: gauge is empty");
            return;
        }

        IsAwakened = true;

        if (animator != null && awakeningData.animSpeedMultiplier > 0f)
            animator.speed = awakeningData.animSpeedMultiplier;

        if (playerMovement != null)
            playerMovement.SetMoveSpeedMultiplier(awakeningData.moveSpeedMultiplier);

        Debug.Log($"[AwakeningManager] Awakening activated! Duration remaining: {currentResourceTime:F1}s");
        OnAwakeningStarted?.Invoke();
    }

    public void DeactivateAwakening()
    {
        if (!IsAwakened) return;

        IsAwakened = false;

        // Start activation cooldown to prevent rapid toggle spamming
        if (awakeningData != null)
        {
            remainingCooldown = awakeningData.activationCooldown;
        }

        if (animator != null)
            animator.speed = 1f;

        if (playerMovement != null)
            playerMovement.SetMoveSpeedMultiplier(1f);

        Debug.Log($"[AwakeningManager] Awakening deactivated. Remaining gauge preserved: {currentResourceTime:F1}s. Cooldown started: {remainingCooldown:F1}s");
        OnAwakeningEnded?.Invoke();
    }

    private void Update()
    {
        if (remainingCooldown > 0f)
        {
            remainingCooldown -= Time.deltaTime;
            if (remainingCooldown < 0f) remainingCooldown = 0f;
            OnCooldownChanged?.Invoke(remainingCooldown);
        }

        if (!IsAwakened) return;

        currentResourceTime -= Time.deltaTime;
        OnGaugeChanged?.Invoke(currentResourceTime, MaxResourceTime);

        if (currentResourceTime <= 0f)
        {
            currentResourceTime = 0f;
            DeactivateAwakening();
        }
    }
}
