using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    public Image fillImage;

    private IHealth targetHealth;

    public void SetTarget(IHealth health)
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateHealth;
        }

        targetHealth = health;

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += UpdateHealth;

            UpdateHealth(
                targetHealth.GetCurrentHealth(),
                targetHealth.GetMaxHealth()
            );
        }
    }

    void UpdateHealth(float current, float max)
    {
        float value = current / max;
        fillImage.fillAmount = value;
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateHealth;
        }
    }
}