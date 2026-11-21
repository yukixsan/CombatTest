using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ComboCountManager : MonoBehaviour
{
    public static ComboCountManager Instance { get; private set; }

    /// <summary>Fires each time hitCount crosses a milestone (every N hits).
    /// Arg: total hit count at the milestone.</summary>
    public event System.Action<int> OnComboMilestone;

    [Header("UI Text (Dual-Layer)")]
    [SerializeField] private TMP_Text shadowText;
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private TMP_Text comboText; // Fallback / legacy slot
    [SerializeField] private TMP_Text hitLabel;  // Separate "HIT" text
    [SerializeField] private string countFormat = "{0}";

    [Header("UI Containers")]
    [SerializeField] private Image timerFillImage;
    [SerializeField] private RectTransform panelRoot;

    [Header("Masked Box Animation")]
    [SerializeField] private Image maskedBoxImage;
    [SerializeField] private Image maskedBoxImage2;
    [SerializeField] private float pulseMinScale = 0.85f;
    [SerializeField] private float pulseMaxScale = 1.25f;
    [SerializeField] private float pulseDuration = 0.4f;
    [SerializeField] private float rotationDuration = 2.0f;
    [SerializeField] private Color[] tierColors = new Color[]
    {
        Color.white,
        new Color(1f, 0.55f, 0f),
        new Color(1f, 0.2f, 0.2f)
    };
    [SerializeField] private int hitsPerColorChange = 10;
    [SerializeField] private float colorTransitionDuration = 0.25f;

    [Header("Fire Particle System")]
    [SerializeField] private ParticleSystem fireParticleSystem;
    [SerializeField] private float[] tierSpeeds = new float[] { 5f, 7f, 10f };
    [SerializeField] private float[] tierEmissionRates = new float[] { 5f, 7f, 10f };

    [Header("Milestone")]
    [SerializeField] private int burstMilestoneInterval = 10;

    [Header("Timing")]
    [SerializeField] private float comboTimeout = 1.5f;

    [Header("Panel Animation")]
    [SerializeField] private Vector2 hiddenAnchoredPos = new Vector2(400f, 0f);
    [SerializeField] private Vector2 shownAnchoredPos = Vector2.zero;
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float panelSlideOutDuration = 0.25f;

    [Header("Text Punch Animation")]
    [SerializeField] private Vector3 textPunchPosition = new Vector3(0f, 15f, 0f);
    [SerializeField] private float textPunchDuration = 0.15f;
    [SerializeField] private int textPunchVibrato = 2;
    [SerializeField] private float textPunchElasticity = 0.5f;

    [Header("Burst Mark Stamp & Reset Animation")]
    [SerializeField] private RectTransform[] burstDropObjects;
    [SerializeField] private float stampStartScale = 2.5f;
    [SerializeField] private float stampDuration = 0.22f;
    [SerializeField] private Ease stampEase = Ease.InQuad;
    [Tooltip("Delay before the second/subsequent elements appear after the first element.")]
    [SerializeField] private float delayAfterFirstElement = 0.15f;
    [SerializeField] private float stampStagger = 0.08f;
    [SerializeField] private float burstStayDuration = 1.2f;
    [SerializeField] private float burstSlideOutDuration = 0.25f;
    [SerializeField] private Ease burstExitEase = Ease.InCubic;

    private int hitCount;
    private int _lastMilestone;
    private int _lastColorTier = -1;
    private Tween fillTween;
    private Tween panelTween;
    private Tween textTween;
    private Tween boxScaleTween;
    private Tween boxRotateTween;
    private Tween boxColorTween;
    private Tween boxScaleTween2;
    private Tween boxRotateTween2;
    private Tween boxColorTween2;
    private Tween timerColorTween;
    private Tween textPunchTween;
    private Tween hitPunchTween;

    private Vector2 _initialTextPos;
    private Vector2 _initialHitPos;
    private Vector2[] _initialDropPositions;
    private Tween[] _dropTweens;
    private Tween[] _exitTweens;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition = hiddenAnchoredPos;
            panelRoot.gameObject.SetActive(false);
        }

        if (shadowText != null)
        {
            _initialTextPos = shadowText.rectTransform.anchoredPosition;
        }
        else if (comboText != null)
        {
            _initialTextPos = comboText.rectTransform.anchoredPosition;
        }

        if (hitLabel != null)
        {
            _initialHitPos = hitLabel.rectTransform.anchoredPosition;
        }

        if (burstDropObjects != null && burstDropObjects.Length > 0)
        {
            _initialDropPositions = new Vector2[burstDropObjects.Length];
            _dropTweens = new Tween[burstDropObjects.Length];
            _exitTweens = new Tween[burstDropObjects.Length];
            for (int i = 0; i < burstDropObjects.Length; i++)
            {
                if (burstDropObjects[i] != null)
                {
                    _initialDropPositions[i] = burstDropObjects[i].anchoredPosition;
                    burstDropObjects[i].gameObject.SetActive(false);
                }
            }
        }

        if (fireParticleSystem != null)
        {
            fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDestroy()
    {
        fillTween?.Kill();
        panelTween?.Kill();
        textTween?.Kill();
        boxScaleTween?.Kill();
        boxRotateTween?.Kill();
        boxColorTween?.Kill();
        boxScaleTween2?.Kill();
        boxRotateTween2?.Kill();
        boxColorTween2?.Kill();
        timerColorTween?.Kill();
        textPunchTween?.Kill();
        hitPunchTween?.Kill();

        KillBurstTweens();
    }

    public void RegisterHit()
    {
        bool wasZero = hitCount == 0;
        hitCount++;
        UpdateText();
        UpdateColorTier();

        if (burstMilestoneInterval > 0)
        {
            int currentMilestone = hitCount / burstMilestoneInterval;
            if (currentMilestone > _lastMilestone)
            {
                _lastMilestone = currentMilestone;
                OnComboMilestone?.Invoke(hitCount);
                TriggerBurstStamp();
            }
        }

        if (wasZero)
        {
            ShowPanel();
        }
        else
        {
            PunchText();
        }

        RestartTimer();
    }

    private void RestartTimer()
    {
        fillTween?.Kill();

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 1f;
            fillTween = timerFillImage
                .DOFillAmount(0f, comboTimeout)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(OnTimerExpired);
        }
    }

    private void OnTimerExpired()
    {
        HidePanel();
        hitCount = 0;
        _lastMilestone = 0;
        _lastColorTier = -1;
    }

    private void ShowPanel()
    {
        if (panelRoot == null) return;

        panelTween?.Kill();
        panelRoot.gameObject.SetActive(true);
        panelRoot.anchoredPosition = shownAnchoredPos;
        panelRoot.localScale = Vector3.zero;

        panelTween = panelRoot
            .DOScale(Vector3.one, panelPopDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        if (tierColors != null && tierColors.Length > 0)
        {
            _lastColorTier = Mathf.Clamp(hitCount / hitsPerColorChange, 0, tierColors.Length - 1);
            Color startColor = tierColors[_lastColorTier];
            if (maskedBoxImage != null) maskedBoxImage.color = startColor;
            if (maskedBoxImage2 != null) maskedBoxImage2.color = startColor;
            if (timerFillImage != null) timerFillImage.color = startColor;
        }

        if (fireParticleSystem != null)
        {
            ApplyParticleTier(_lastColorTier);
            if (!fireParticleSystem.isPlaying)
                fireParticleSystem.Play();
        }

        StartMaskedBoxLoop();
        HideBurstDrop();
    }

    private void HidePanel()
    {
        if (panelRoot == null) return;

        panelTween?.Kill();
        StopMaskedBoxLoop();
        HideBurstDrop();

        if (fireParticleSystem != null && fireParticleSystem.isPlaying)
        {
            fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        panelTween = panelRoot
            .DOAnchorPos(hiddenAnchoredPos, panelSlideOutDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                panelRoot.gameObject.SetActive(false);
                panelRoot.anchoredPosition = hiddenAnchoredPos;
            });
    }

    private void StartMaskedBoxLoop()
    {
        StartSingleBoxLoop(maskedBoxImage, ref boxScaleTween, ref boxRotateTween);
        StartSingleBoxLoop(maskedBoxImage2, ref boxScaleTween2, ref boxRotateTween2);
    }

    private void StartSingleBoxLoop(Image box, ref Tween scaleTween, ref Tween rotateTween)
    {
        if (box == null) return;

        scaleTween?.Kill();
        rotateTween?.Kill();

        box.transform.localScale = Vector3.one * pulseMinScale;
        scaleTween = box.transform
            .DOScale(pulseMaxScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);

        box.transform.localRotation = Quaternion.identity;
        rotateTween = box.transform
            .DORotate(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    private void StopMaskedBoxLoop()
    {
        boxScaleTween?.Kill();
        boxRotateTween?.Kill();
        boxColorTween?.Kill();
        boxScaleTween2?.Kill();
        boxRotateTween2?.Kill();
        boxColorTween2?.Kill();
        timerColorTween?.Kill();

        if (maskedBoxImage != null)
        {
            maskedBoxImage.transform.localScale = Vector3.one;
            maskedBoxImage.transform.localRotation = Quaternion.identity;
        }

        if (maskedBoxImage2 != null)
        {
            maskedBoxImage2.transform.localScale = Vector3.one;
            maskedBoxImage2.transform.localRotation = Quaternion.identity;
        }
    }

    private void UpdateColorTier()
    {
        if (tierColors == null || tierColors.Length == 0) return;

        // Clamped at max tier so it stays on the top tier instead of recycling back
        int tierIndex = Mathf.Clamp(hitCount / hitsPerColorChange, 0, tierColors.Length - 1);
        if (tierIndex != _lastColorTier)
        {
            _lastColorTier = tierIndex;
            Color targetColor = tierColors[tierIndex];

            if (maskedBoxImage != null)
            {
                boxColorTween?.Kill();
                boxColorTween = maskedBoxImage
                    .DOColor(targetColor, colorTransitionDuration)
                    .SetUpdate(true);
            }

            if (maskedBoxImage2 != null)
            {
                boxColorTween2?.Kill();
                boxColorTween2 = maskedBoxImage2
                    .DOColor(targetColor, colorTransitionDuration)
                    .SetUpdate(true);
            }

            if (timerFillImage != null)
            {
                timerColorTween?.Kill();
                timerColorTween = timerFillImage
                    .DOColor(targetColor, colorTransitionDuration)
                    .SetUpdate(true);
            }

            ApplyParticleTier(tierIndex);
        }
    }

    private void ApplyParticleTier(int tierIndex)
    {
        if (fireParticleSystem == null) return;

        var main = fireParticleSystem.main;
        var emission = fireParticleSystem.emission;

        // Tier start color
        if (tierColors != null && tierColors.Length > 0)
        {
            int colorIdx = Mathf.Clamp(tierIndex, 0, tierColors.Length - 1);
            main.startColor = new ParticleSystem.MinMaxGradient(tierColors[colorIdx]);
        }

        // Tier speed (default: 5, 7, 10)
        if (tierSpeeds != null && tierSpeeds.Length > 0)
        {
            int speedIdx = Mathf.Clamp(tierIndex, 0, tierSpeeds.Length - 1);
            main.startSpeed = tierSpeeds[speedIdx];
        }

        // Tier emission rate over time (default: 5, 7, 10)
        if (tierEmissionRates != null && tierEmissionRates.Length > 0)
        {
            int emitIdx = Mathf.Clamp(tierIndex, 0, tierEmissionRates.Length - 1);
            emission.rateOverTime = tierEmissionRates[emitIdx];
        }
    }

    private void PunchText()
    {
        RectTransform target = shadowText != null ? shadowText.rectTransform : (comboText != null ? comboText.rectTransform : null);
        if (target != null)
        {
            textPunchTween?.Kill();
            target.anchoredPosition = _initialTextPos;
            textPunchTween = target
                .DOPunchPosition(textPunchPosition, textPunchDuration, textPunchVibrato, textPunchElasticity)
                .SetUpdate(true);
        }

        if (hitLabel != null)
        {
            hitPunchTween?.Kill();
            hitLabel.rectTransform.anchoredPosition = _initialHitPos;
            hitPunchTween = hitLabel.rectTransform
                .DOPunchPosition(textPunchPosition, textPunchDuration, textPunchVibrato, textPunchElasticity)
                .SetUpdate(true);
        }
    }

    private void UpdateText()
    {
        string textStr = hitCount > 0 ? string.Format(countFormat, hitCount) : "";

        if (shadowText != null)
            shadowText.text = textStr;

        if (mainText != null)
            mainText.text = textStr;

        if (comboText != null)
            comboText.text = textStr;
    }

    private void TriggerBurstStamp()
    {
        if (burstDropObjects == null || burstDropObjects.Length == 0) return;

        KillBurstTweens();

        float maxEntranceTime = 0f;

        for (int i = 0; i < burstDropObjects.Length; i++)
        {
            var obj = burstDropObjects[i];
            if (obj == null) continue;

            obj.gameObject.SetActive(true);
            obj.anchoredPosition = _initialDropPositions[i];
            obj.localScale = Vector3.one * stampStartScale;

            float delay = i == 0 ? 0f : delayAfterFirstElement + (i - 1) * stampStagger;
            float totalEntranceTime = delay + stampDuration;
            if (totalEntranceTime > maxEntranceTime)
                maxEntranceTime = totalEntranceTime;

            // Zoom in from large to 1.0 (slam/stamp effect)
            _dropTweens[i] = obj
                .DOScale(Vector3.one, stampDuration)
                .SetDelay(delay)
                .SetEase(stampEase)
                .SetUpdate(true);
        }

        // Schedule reset slide out after staying for burstStayDuration
        float exitDelay = maxEntranceTime + burstStayDuration;
        for (int i = 0; i < burstDropObjects.Length; i++)
        {
            int index = i;
            var obj = burstDropObjects[i];
            if (obj == null) continue;

            Vector2 exitTarget = _initialDropPositions[index] + hiddenAnchoredPos;
            _exitTweens[index] = obj
                .DOAnchorPos(exitTarget, burstSlideOutDuration)
                .SetDelay(exitDelay)
                .SetEase(burstExitEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    obj.gameObject.SetActive(false);
                    obj.anchoredPosition = _initialDropPositions[index];
                    obj.localScale = Vector3.one;
                });
        }
    }

    private void KillBurstTweens()
    {
        if (_dropTweens != null)
        {
            for (int i = 0; i < _dropTweens.Length; i++)
            {
                _dropTweens[i]?.Kill();
            }
        }

        if (_exitTweens != null)
        {
            for (int i = 0; i < _exitTweens.Length; i++)
            {
                _exitTweens[i]?.Kill();
            }
        }
    }

    private void HideBurstDrop()
    {
        if (burstDropObjects == null) return;

        KillBurstTweens();

        for (int i = 0; i < burstDropObjects.Length; i++)
        {
            var obj = burstDropObjects[i];
            if (obj == null) continue;

            obj.anchoredPosition = _initialDropPositions != null && i < _initialDropPositions.Length
                ? _initialDropPositions[i]
                : obj.anchoredPosition;
            obj.localScale = Vector3.one;
            obj.gameObject.SetActive(false);
        }
    }
}
