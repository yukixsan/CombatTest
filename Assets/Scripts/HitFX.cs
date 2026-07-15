using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class HitFX : MonoBehaviour
{
    [Header("SHAKE")]
    public bool shake;
    public float duration = 0.15f;
    public float magnitude = 0.1f;
    public float shakeFrequency = 8f;
    [SerializeField] private Transform targetTransform;

    [Header("BLINK")]
    public Color blinkColor = Color.white;
    public float blinkSpeed = 0.05f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private Vector3 originalTargetPos;

    public Renderer[] renderers;
    private Color[] originalColors;
    private string[] colorPropertyNames;


    [Header("EVENT")]
    public UnityEvent OnActive;
    public UnityEvent OnDisable;

    private void Awake()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        originalTargetPos = targetTransform != null ? targetTransform.localPosition : originalPos;

        originalColors = new Color[renderers.Length];
        colorPropertyNames = new string[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            string propName = GetColorPropertyName(renderers[i].material);
            colorPropertyNames[i] = propName;

            if (propName != null)
                originalColors[i] = renderers[i].material.GetColor(propName);
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
                // Deterministic back-and-forth on X axis only (sinusoidal).
                float damper = 1f - (time / duration);
                float x = Mathf.Sin(time * shakeFrequency) * magnitude * damper * 0.25f;
                Vector3 posOffset = new Vector3(x, 0f, 0f);

                if (targetTransform != null)
                    targetTransform.localPosition = originalTargetPos + posOffset;
                else
                    transform.localPosition = originalPos + posOffset;
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
            if (targetTransform != null)
                targetTransform.localPosition = originalTargetPos;
            else
                transform.localPosition = originalPos;

            transform.localRotation = originalRot;
        }
        SetOriginalColor();
    }
    private string GetColorPropertyName(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return "_BaseColor"; // URP / custom lit shaders
        if (mat.HasProperty("_Color")) return "_Color";           // Built-in RP
        if (mat.HasProperty("_TintColor")) return "_TintColor";   // some custom shaders
        return null;
    }

    private void SetColor(Color color)
    {
         for (int i = 0; i < renderers.Length; i++)
        {
            if (colorPropertyNames[i] != null)
                renderers[i].material.SetColor(colorPropertyNames[i], color);
        }
    }

    private void SetOriginalColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (colorPropertyNames[i] != null)
                renderers[i].material.SetColor(colorPropertyNames[i], originalColors[i]);
        }
    }
}