using TMPro;
using UnityEngine;

/// <summary>
/// Minimal burst gauge UI — shows "Burst: 3/5" as TMP_Text.
/// </summary>
public class UIBurstGauge : MonoBehaviour
{
    [SerializeField] private TMP_Text gaugeText;
    [SerializeField] private BurstGauge burstGauge; // wire in inspector or call SetTarget

    private void OnEnable()
    {
        if (burstGauge != null)
        {
            burstGauge.OnGaugeChanged += UpdateText;
            UpdateText(burstGauge.CurrentGauge, burstGauge.MaxGauge);
        }
    }

    private void Start()
    {
        if (burstGauge != null)
            UpdateText(burstGauge.CurrentGauge, burstGauge.MaxGauge);
    }

    private void OnDisable()
    {
        if (burstGauge != null)
            burstGauge.OnGaugeChanged -= UpdateText;
    }

    public void SetTarget(BurstGauge target)
    {
        if (burstGauge != null)
            burstGauge.OnGaugeChanged -= UpdateText;

        burstGauge = target;

        if (burstGauge != null)
        {
            burstGauge.OnGaugeChanged += UpdateText;
            UpdateText(burstGauge.CurrentGauge, burstGauge.MaxGauge);
        }
    }

    private void UpdateText(int current, int max)
    {
        if (gaugeText != null)
            gaugeText.text = $"{current}/{max}";
    }
}
