using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class TutorialPanelView : MonoBehaviour
{
    [Tooltip("Hit markers in the SAME ORDER as this step's requiredSequence. " +
             "Leave empty for single-input steps that don't need sequence highlighting.")]
    [SerializeField] private List<Image> hitMarkers = new();

    [Header("Punch")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private int punchVibrato = 6;

    [Header("Alpha")]
    [SerializeField] private float dimAlpha = 0.3f;
    [SerializeField] private float litAlpha = 1f;

    private Vector3[] _baseScales;

    private void Awake()
    {
        _baseScales = new Vector3[hitMarkers.Count];
        for (int i = 0; i < hitMarkers.Count; i++)
        {
            if (hitMarkers[i] != null)
                _baseScales[i] = hitMarkers[i].transform.localScale;
        }
    }

    /// <summary>
    /// completedCount = how many sequence entries have been successfully hit so far.
    /// Marker [completedCount - 1] gets punched; markers [0..completedCount-1] go lit,
    /// the rest stay/are set dim.
    /// </summary>
    public void SetHighlightIndex(int completedCount)
    {
        if (hitMarkers.Count == 0) return;

        for (int i = 0; i < hitMarkers.Count; i++)
        {
            if (hitMarkers[i] == null) continue;

            bool isLit = i < completedCount;
            SetAlpha(hitMarkers[i], isLit ? litAlpha : dimAlpha);
        }

        int justHitIndex = completedCount - 1;
        if (justHitIndex >= 0 && justHitIndex < hitMarkers.Count)
        {
            PunchMarker(justHitIndex);
        }
    }

    /// <summary>
    /// Resets all markers to dim, no punch. Used on full-sequence completion
    /// (before advancing/deactivating) and on fail/timeout (progress back to 0).
    /// </summary>
    public void ResetMarkers()
    {
        for (int i = 0; i < hitMarkers.Count; i++)
        {
            if (hitMarkers[i] == null) continue;
            SetAlpha(hitMarkers[i], dimAlpha);
            hitMarkers[i].transform.localScale = _baseScales[i]; // snap back in case a punch was mid-flight
        }
    }

    private void PunchMarker(int index)
    {
        var marker = hitMarkers[index];
        if (marker == null) return;

        marker.transform.DOKill(); // avoid stacking punches if hit again before finishing
        marker.transform.localScale = _baseScales[index];
        marker.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, punchVibrato);
    }

    private void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}