using System;
using UnityEngine;

/// <summary>
/// Burst gauge resource. Lives on the Player GO.
/// PlayerCombat and UI subscribe to OnGaugeChanged.
/// </summary>
public class BurstGauge : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxGauge = 5;

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentGauge;

    public int CurrentGauge => currentGauge;
    public int MaxGauge => maxGauge;

    /// <summary>Fires whenever the gauge value changes. Args: (current, max).</summary>
    public event Action<int, int> OnGaugeChanged;

    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_subscribed && ComboCountManager.Instance != null)
        {
            ComboCountManager.Instance.OnComboMilestone -= OnMilestone;
            _subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (ComboCountManager.Instance != null)
        {
            ComboCountManager.Instance.OnComboMilestone += OnMilestone;
            _subscribed = true;
        }
    }

    private void OnMilestone(int totalHits)
    {
        AddGauge(1);
    }

    /// <summary>Add gauge (clamped to max). Called by combo milestone listener.</summary>
    public void AddGauge(int amount)
    {
        int prev = currentGauge;
        currentGauge = Mathf.Min(currentGauge + amount, maxGauge);
        if (currentGauge != prev)
            OnGaugeChanged?.Invoke(currentGauge, maxGauge);
    }

    /// <summary>Try to spend 1 gauge. Returns false if empty.</summary>
    public bool TryConsume()
    {
        if (currentGauge <= 0) return false;
        currentGauge--;
        OnGaugeChanged?.Invoke(currentGauge, maxGauge);
        return true;
    }

    /// <summary>Reset gauge to 0 (e.g. on death).</summary>
    public void ResetGauge()
    {
        currentGauge = 0;
        OnGaugeChanged?.Invoke(currentGauge, maxGauge);
    }
}

