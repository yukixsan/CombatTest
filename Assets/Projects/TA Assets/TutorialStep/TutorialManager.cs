using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private List<TutorialStep> steps = new();

    [Header("Runtime")]
    [SerializeField] private int currentStepIndex = 0;
    [SerializeField] private float sequenceTimeout = 1f;
    private float sequenceTimer;
    [Header("SFXs")]
    [SerializeField] private AudioClip sequenceProgressSFX;
    [SerializeField] private AudioClip stepCompletedSFX;
    [SerializeField] private AudioClip failSFX;
    [SerializeField] private float sfxVolume = 1f;
    public event Action<TutorialStep, int> OnStepProgressChanged; // step, sequenceProgress
    public event Action<TutorialStep> OnStepCompleted;
    public event Action OnTutorialFinished;

    public TutorialStep CurrentStep =>
        (currentStepIndex >= 0 && currentStepIndex < steps.Count) ? steps[currentStepIndex] : null;

    public bool IsFinished => currentStepIndex >= steps.Count;

     private void OnEnable()
    {
        if (playerCombat != null)
            playerCombat.OnActionAccepted += HandleActionAccepted;
    }

    
    private void OnDisable()
    {
        if (playerCombat != null)
            playerCombat.OnActionAccepted -= HandleActionAccepted;
    }

    private void Update()
    {
        var step = CurrentStep;
        if (step == null || step.currentSequenceProgress == 0) return;

        sequenceTimer -= Time.deltaTime;
        if (sequenceTimer <= 0f)
        {
            step.currentSequenceProgress = 0;
            OnStepProgressChanged?.Invoke(step, 0);
            PlaySFXSafe(failSFX);
        }
    }

private void HandleActionAccepted(CombatActionData data)
{
    var step = CurrentStep;
    if (step == null) return; // tutorial already finished

    var expected = step.requiredSequence[step.currentSequenceProgress];

    if (data == expected)
    {
        step.currentSequenceProgress++;
        sequenceTimer = sequenceTimeout; // refresh window for the next expected hit
        OnStepProgressChanged?.Invoke(step, step.currentSequenceProgress);

        if (step.currentSequenceProgress >= step.requiredSequence.Count)
        {
            step.currentSequenceProgress = 0;
            sequenceTimer = 0f;
            step.completionCount++;
            PlaySFXSafe(sequenceProgressSFX);

            if (step.IsComplete)
            {
                AdvanceStep();
                OnStepCompleted?.Invoke(step);
                PlaySFXSafe(stepCompletedSFX);
            }
            else
            {
                OnStepProgressChanged?.Invoke(step, 0);
            }
        }
    }
    else
    {
        // Mismatch — reset sequence progress, don't fail the tutorial.
        if (step.currentSequenceProgress != 0)
        {
            step.currentSequenceProgress = 0;
            sequenceTimer = 0f;
            OnStepProgressChanged?.Invoke(step, 0);
            PlaySFXSafe(failSFX);
        }
    }
}

    private void AdvanceStep()
    {
        currentStepIndex++;
        if (IsFinished)
        {
            OnTutorialFinished?.Invoke();
        }
    }

    // Call this if you need to restart the tutorial (e.g. on scene re-entry)
    public void ResetTutorial()
    {
        currentStepIndex = 0;
        sequenceTimer = 0f;
        foreach (var step in steps)
        {
            step.currentSequenceProgress = 0;
            step.completionCount = 0;
        }
    }
    private void PlaySFXSafe(AudioClip clip)
{
    if (clip == null) return;
    if (SFXManager.Instance == null) return;
    SFXManager.Instance.PlaySFX(clip, sfxVolume);
}
}

