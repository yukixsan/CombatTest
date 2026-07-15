using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ComboCountManager : MonoBehaviour
{
    public static ComboCountManager Instance { get; private set; }
     [Header("UI")]
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private RectTransform panelRoot;

    [Header("Timing")]
    [SerializeField] private float comboTimeout = 1.5f;

    [Header("Panel Animation")]
    [SerializeField] private Vector2 hiddenAnchoredPos = new Vector2(400f, 0f);
    [SerializeField] private Vector2 shownAnchoredPos = Vector2.zero;
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float panelSlideOutDuration = 0.25f;

    [Header("Text Pop Animation")]
    [SerializeField] private float textPunchScale = 0.3f;
    [SerializeField] private float textPunchDuration = 0.2f;

    private int hitCount;
    private Tween fillTween;
    private Tween panelTween;
    private Tween textTween;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition = hiddenAnchoredPos;
            panelRoot.gameObject.SetActive(false);
        }
    }

    public void RegisterHit()
    {
        bool wasZero = hitCount == 0;
        hitCount++;
        UpdateText();

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
                .OnComplete(OnTimerExpired);
        }
    }

    private void OnTimerExpired()
    {
        HidePanel();
        hitCount = 0;
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
            .SetEase(Ease.OutBack);
    }

    private void HidePanel()
    {
        if (panelRoot == null) return;

        panelTween?.Kill();
        panelTween = panelRoot
            .DOAnchorPos(hiddenAnchoredPos, panelSlideOutDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                panelRoot.gameObject.SetActive(false);
                panelRoot.anchoredPosition = hiddenAnchoredPos;
            });
    }

    private void PunchText()
    {
        if (comboText == null) return;

        textTween?.Kill();
        comboText.transform.localScale = Vector3.one;
        textTween = comboText.transform
            .DOPunchScale(Vector3.one * textPunchScale, textPunchDuration, vibrato: 1, elasticity: 0.5f);
    }

    private void UpdateText()
    {
        if (comboText == null) return;
        comboText.text = hitCount > 0 ? $"{hitCount} x" : "";
    }
}
