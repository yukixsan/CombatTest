using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class HitFX : MonoBehaviour
{
    [Header("SHAKE")]
    public bool shake;
    public float duration = 0.15f;
    public float magnitude = 0.1f;

    [Header("BLINK")]
    public Color blinkColor = Color.white;
    public float blinkSpeed = 0.05f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    public Renderer[] renderers;
    private Color[] originalColors;

    [Header("EVENT")]
    public UnityEvent OnActive;
    public UnityEvent OnDisable;

    private void Awake()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
        }
    }

    public void PlayEffect()
    {
        StopAllCoroutines();
        StartCoroutine(DoEffect());
    }

    private IEnumerator DoEffect()
    {
        float time = 0f;

        while (time < duration)
        {
            if (shake)
            {
                float damper = 1f - (time / duration);

                Vector3 posOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * magnitude * damper;

                Vector3 rotOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * magnitude * 10f * damper;

                transform.localPosition = originalPos + posOffset;
                transform.localRotation = originalRot * Quaternion.Euler(rotOffset);
            }
            
            SetColor(blinkColor);
            OnActive?.Invoke();

            yield return new WaitForSeconds(blinkSpeed);

            SetOriginalColor();

            yield return new WaitForSeconds(blinkSpeed);

            time += blinkSpeed * 2f;
            OnDisable?.Invoke();
        }

        if (shake)
        {
            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
        }
        SetOriginalColor();
    }

    private void SetColor(Color color)
    {
        foreach (var rend in renderers)
        {
            if (rend.material.HasProperty("_Color"))
                rend.material.color = color;
        }
    }

    private void SetOriginalColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
    }
}