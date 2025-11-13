using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }
    bool _isHitstopActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void StartHitstop(float duration)
    {
        if (_isHitstopActive) return; // prevent stacking
        StartCoroutine(HitstopCoroutine(duration));
    }

    IEnumerator HitstopCoroutine(float duration)
    {
        _isHitstopActive = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        _isHitstopActive = false;
    }
}
