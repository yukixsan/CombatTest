
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text completionText;

    private void OnEnable()
    {
        tutorialManager.OnStepProgressChanged += HandleProgress;
        tutorialManager.OnStepCompleted += HandleStepCompleted;
        tutorialManager.OnTutorialFinished += HandleFinished;

        RefreshInstruction();
    }

    private void OnDisable()
    {
        tutorialManager.OnStepProgressChanged -= HandleProgress;
        tutorialManager.OnStepCompleted -= HandleStepCompleted;
        tutorialManager.OnTutorialFinished -= HandleFinished;
    }

    private void RefreshInstruction()
    {
        var step = tutorialManager.CurrentStep;
        instructionText.text = step != null ? step.displayText : "";
    }

    private void HandleProgress(TutorialStep step, int sequenceProgress)
    {
        instructionText.text =
            $"{step.displayText}\n({sequenceProgress}/{step.requiredSequence.Count} — {step.completionCount}/{step.requiredCompletions} completions)";
    }

    private void HandleStepCompleted(TutorialStep step)
    {
        RefreshInstruction();
    }

    private void HandleFinished()
    {
        instructionText.text = "";
        completionText.text = "Tutorial Complete!";
    }
}