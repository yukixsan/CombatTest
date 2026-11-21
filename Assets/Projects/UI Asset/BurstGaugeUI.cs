using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit runtime controller for the Burst Mark HUD.
/// Listens to BurstGauge and drives:
/// 1. Springy upward intro on gain.
/// 2. Active idle animation: front breathing scale pulse + vertical float bob.
/// 3. Sibling companion glow pulsing in scale and opacity.
/// 4. Upward energy dissipation fade-out on depletion.
/// </summary>
public class BurstGaugeUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private BurstGauge burstGauge;

    [Header("Idle Bob & Breathe (Front Mark)")]
    [Tooltip("Vertical floating bob distance in pixels.")]
    [SerializeField] private float bobHeight = 2.0f;
    [Tooltip("Speed of the floating oscillation.")]
    [SerializeField] private float bobSpeed = 2.5f;
    [Tooltip("Scale breathing expansion (1.0 to 1.0 + breatheScale).")]
    [SerializeField] private float breatheScale = 0.08f;
    [Tooltip("Phase offset between marks for a wave effect.")]
    [SerializeField] private float phaseOffset = 0.6f;

    [Header("Pulsing Glow (Behind Layer)")]
    [SerializeField] private float glowMinAlpha = 0.2f;
    [SerializeField] private float glowMaxAlpha = 0.85f;
    [SerializeField] private float glowPulseSpeed = 3.5f;
    [SerializeField] private float glowScaleMultiplier = 1.15f;

    [Header("Gain / Intro Animation")]
    [Tooltip("Duration for the mark to rise and shake into place.")]
    [SerializeField] private float gainDuration = 0.35f;
    [Tooltip("Vertical distance below the center the mark starts from.")]
    [SerializeField] private float gainRiseDistance = 20f;
    [Tooltip("Burst shake intensity during arrival.")]
    [SerializeField] private float gainShakeIntensity = 3.5f;
    [Tooltip("Scale pop multiplier during arrival.")]
    [SerializeField] private float gainScalePop = 1.25f;

    [Header("Spend / Deplete Animation")]
    [Tooltip("Duration for the mark to float upward and fade out when spent.")]
    [SerializeField] private float depleteDuration = 0.4f;
    [Tooltip("Distance the mark rises upward as it dissipates.")]
    [SerializeField] private float depleteRiseDistance = 20f;

    private VisualElement[] _marks = new VisualElement[3];
    private VisualElement[] _glows = new VisualElement[3];
    private Coroutine[] _gainCoroutines = new Coroutine[3];
    private Coroutine[] _depleteCoroutines = new Coroutine[3];
    private bool[] _isGaining = new bool[3];
    private bool[] _isDepleting = new bool[3];

    private int _currentGauge;
    private int _lastGauge;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        BindElements();
    }

    private void OnEnable()
    {
        if (burstGauge == null)
            burstGauge = FindAnyObjectByType<BurstGauge>();

        if (burstGauge != null)
        {
            burstGauge.OnGaugeChanged += UpdateMarks;
            _lastGauge = burstGauge.CurrentGauge;
            _currentGauge = _lastGauge;
            ApplyInitialState(_currentGauge);
        }
    }

    private void Start()
    {
        if (_marks[0] == null)
            BindElements();

        if (burstGauge != null)
        {
            _lastGauge = burstGauge.CurrentGauge;
            _currentGauge = _lastGauge;
            ApplyInitialState(_currentGauge);
        }
    }

    private void OnDisable()
    {
        if (burstGauge != null)
            burstGauge.OnGaugeChanged -= UpdateMarks;

        StopAllCoroutines();
        for (int i = 0; i < 3; i++)
        {
            _isDepleting[i] = false;
            _isGaining[i] = false;
            _gainCoroutines[i] = null;
            _depleteCoroutines[i] = null;
        }
    }

    public void SetTarget(BurstGauge target)
    {
        if (burstGauge != null)
            burstGauge.OnGaugeChanged -= UpdateMarks;

        burstGauge = target;

        if (burstGauge != null)
        {
            burstGauge.OnGaugeChanged += UpdateMarks;
            _lastGauge = burstGauge.CurrentGauge;
            _currentGauge = _lastGauge;
            ApplyInitialState(_currentGauge);
        }
    }

    private void BindElements()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        _marks[0] = root.Q("Mark1");
        _marks[1] = root.Q("Mark2");
        _marks[2] = root.Q("Mark3");

        _glows[0] = root.Q("Mark1_Glow");
        _glows[1] = root.Q("Mark2_Glow");
        _glows[2] = root.Q("Mark3_Glow");
    }

    private void ApplyInitialState(int gauge)
    {
        for (int i = 0; i < 3; i++)
        {
            bool active = i < gauge;
            _marks[i]?.EnableInClassList("burst-mark--active", active);
            _marks[i]?.EnableInClassList("burst-mark--inactive", !active);
            _glows[i]?.EnableInClassList("burst-mark--active", active);
            _glows[i]?.EnableInClassList("burst-mark--inactive", !active);

            if (!active)
            {
                if (_marks[i] != null)
                {
                    _marks[i].transform.position = Vector3.zero;
                    _marks[i].style.scale = new StyleScale(new Scale(Vector2.one));
                }
                if (_glows[i] != null)
                {
                    _glows[i].transform.position = Vector3.zero;
                    _glows[i].style.scale = new StyleScale(new Scale(Vector2.one));
                }
            }
        }
    }

    private void Update()
    {
        if (_currentGauge <= 0) return;

        float time = Time.unscaledTime;
        int activeCount = Mathf.Min(_currentGauge, 3);

        for (int i = 0; i < activeCount; i++)
        {
            if (_isDepleting[i] || _isGaining[i]) continue;

            var mark = _marks[i];
            var glow = _glows[i];
            if (mark == null) continue;

            float phase = time * bobSpeed + i * phaseOffset;
            float yBob = Mathf.Sin(phase) * bobHeight;

            // Front mark: gentle hover float + breathing scale
            float markScaleNorm = Mathf.Sin(phase) * 0.5f + 0.5f;
            float currentMarkScale = 1f + markScaleNorm * breatheScale;
            mark.transform.position = new Vector3(0f, yBob, 0f);
            mark.style.scale = new StyleScale(new Scale(new Vector2(currentMarkScale, currentMarkScale)));

            // Glow layer: float with mark + pulsing scale & opacity
            if (glow != null)
            {
                float glowPhase = time * glowPulseSpeed + i * phaseOffset;
                float glowNorm = Mathf.Sin(glowPhase) * 0.5f + 0.5f;
                float currentGlowScale = 1f + glowNorm * (glowScaleMultiplier - 1f);

                glow.transform.position = new Vector3(0f, yBob, 0f);
                glow.style.scale = new StyleScale(new Scale(new Vector2(currentGlowScale, currentGlowScale)));
                glow.style.opacity = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, glowNorm);
            }
        }
    }

    private void UpdateMarks(int current, int max)
    {
        _currentGauge = current;

        // Gain marks: play upward intro with burst shake on each newly gained slot
        if (current > _lastGauge)
        {
            int startSlot = Mathf.Clamp(_lastGauge, 0, 3);
            int endSlot = Mathf.Clamp(current, 0, 3);

            for (int i = startSlot; i < endSlot; i++)
            {
                if (_depleteCoroutines[i] != null)
                {
                    StopCoroutine(_depleteCoroutines[i]);
                    _depleteCoroutines[i] = null;
                    _isDepleting[i] = false;
                }

                if (_gainCoroutines[i] != null)
                {
                    StopCoroutine(_gainCoroutines[i]);
                    _gainCoroutines[i] = null;
                }

                _gainCoroutines[i] = StartCoroutine(AnimateGain(i));
            }
        }
        // Spend marks: dissipate upward from highest active slot down
        else if (current < _lastGauge)
        {
            int startSlot = Mathf.Clamp(_lastGauge - 1, 0, 2);
            int endSlot = Mathf.Clamp(current, 0, 3);

            for (int i = startSlot; i >= endSlot; i--)
            {
                if (_gainCoroutines[i] != null)
                {
                    StopCoroutine(_gainCoroutines[i]);
                    _gainCoroutines[i] = null;
                    _isGaining[i] = false;
                }

                if (_depleteCoroutines[i] != null)
                    StopCoroutine(_depleteCoroutines[i]);

                _depleteCoroutines[i] = StartCoroutine(AnimateDeplete(i));
            }
        }

        _lastGauge = current;
    }

    private IEnumerator AnimateGain(int slotIndex)
    {
        _isGaining[slotIndex] = true;

        var mark = _marks[slotIndex];
        var glow = _glows[slotIndex];

        // Ensure active class is enabled
        mark?.RemoveFromClassList("burst-mark--inactive");
        mark?.AddToClassList("burst-mark--active");
        glow?.RemoveFromClassList("burst-mark--inactive");
        glow?.AddToClassList("burst-mark--active");

        float elapsed = 0f;

        while (elapsed < gainDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / gainDuration);

            // Upward rise: fast approach easing into diamond socket
            float riseEased = 1f - Mathf.Pow(1f - t, 3f);
            float baseY = Mathf.Lerp(gainRiseDistance, 0f, riseEased);

            // Decaying shake on arrival
            float decay = 1f - t;
            float shakeX = Mathf.Sin(t * Mathf.PI * 8f) * gainShakeIntensity * decay;
            float shakeY = Mathf.Cos(t * Mathf.PI * 8f) * (gainShakeIntensity * 0.5f) * decay;

            // Scale pop (punches up to gainScalePop then settles to 1.0)
            float popScale = Mathf.Lerp(0.85f, 1f, riseEased) + Mathf.Sin(t * Mathf.PI) * (gainScalePop - 1f);

            // Quick opacity fade-in
            float alpha = Mathf.Clamp01(t / 0.25f);

            if (mark != null)
            {
                mark.transform.position = new Vector3(shakeX, baseY + shakeY, 0f);
                mark.style.scale = new StyleScale(new Scale(new Vector2(popScale, popScale)));
                mark.style.opacity = alpha;
            }

            if (glow != null)
            {
                float glowPopScale = popScale * glowScaleMultiplier;
                glow.transform.position = new Vector3(shakeX, baseY + shakeY, 0f);
                glow.style.scale = new StyleScale(new Scale(new Vector2(glowPopScale, glowPopScale)));
                glow.style.opacity = alpha * Mathf.Lerp(glowMaxAlpha, glowMinAlpha, t);
            }

            yield return null;
        }

        // Clean settle into resting diamond position
        if (mark != null)
        {
            mark.transform.position = Vector3.zero;
            mark.style.scale = new StyleScale(new Scale(Vector2.one));
            mark.style.opacity = StyleKeyword.Null; // Hand authority back to USS class
        }

        if (glow != null)
        {
            glow.transform.position = Vector3.zero;
            glow.style.scale = new StyleScale(new Scale(Vector2.one));
            glow.style.opacity = StyleKeyword.Null;
        }

        _isGaining[slotIndex] = false;
        _gainCoroutines[slotIndex] = null;
    }

    private IEnumerator AnimateDeplete(int slotIndex)
    {
        _isDepleting[slotIndex] = true;

        var mark = _marks[slotIndex];
        var glow = _glows[slotIndex];

        // Remove active class so it is no longer anchored to active state
        mark?.RemoveFromClassList("burst-mark--active");
        glow?.RemoveFromClassList("burst-mark--active");

        Vector3 startPos = mark != null ? mark.transform.position : Vector3.zero;
        Vector3 targetPos = startPos - new Vector3(0f, depleteRiseDistance, 0f);

        float elapsed = 0f;

        while (elapsed < depleteDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / depleteDuration);
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth ease-out

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easeT);
            float currentAlpha = Mathf.Lerp(1f, 0f, t);

            if (mark != null)
            {
                mark.transform.position = currentPos;
                mark.style.opacity = currentAlpha;
            }

            if (glow != null)
            {
                glow.transform.position = currentPos;
                glow.style.opacity = currentAlpha * glowMaxAlpha;
            }

            yield return null;
        }

        // Clean reset to base ready state
        if (mark != null)
        {
            mark.transform.position = Vector3.zero;
            mark.style.scale = new StyleScale(new Scale(Vector2.one));
            mark.style.opacity = StyleKeyword.Null; // Hand back control to USS
            mark.AddToClassList("burst-mark--inactive");
        }

        if (glow != null)
        {
            glow.transform.position = Vector3.zero;
            glow.style.scale = new StyleScale(new Scale(Vector2.one));
            glow.style.opacity = StyleKeyword.Null;
            glow.AddToClassList("burst-mark--inactive");
        }

        _isDepleting[slotIndex] = false;
        _depleteCoroutines[slotIndex] = null;
    }
}
