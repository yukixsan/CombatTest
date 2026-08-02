using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [Tooltip("One entry per tutorial step, SAME ORDER as TutorialManager's steps list.")]
    [SerializeField] private List<GameObject> tutorialObjects = new();

    [SerializeField] private TMP_Text completionText; // shows "completionCount / requiredCompletions"

    private int _activeIndex = -1;
    private TutorialPanelView _activePanelView;

    private void OnEnable()
    {
        tutorialManager.OnStepProgressChanged += HandleProgress;
        tutorialManager.OnStepCompleted += HandleStepCompleted;
        tutorialManager.OnTutorialFinished += HandleFinished;

        RefreshActivePanel();
    }

    private void OnDisable()
    {
        tutorialManager.OnStepProgressChanged -= HandleProgress;
        tutorialManager.OnStepCompleted -= HandleStepCompleted;
        tutorialManager.OnTutorialFinished -= HandleFinished;
    }

    private void RefreshActivePanel()
    {
        int newIndex = tutorialManager.CurrentStepIndex;

        if (newIndex != _activeIndex)
        {
            // deactivate old
            if (_activeIndex >= 0 && _activeIndex < tutorialObjects.Count && tutorialObjects[_activeIndex] != null)
                tutorialObjects[_activeIndex].SetActive(false);

            _activeIndex = newIndex;
            _activePanelView = null;

            // activate new
            if (_activeIndex >= 0 && _activeIndex < tutorialObjects.Count && tutorialObjects[_activeIndex] != null)
            {
                var panel = tutorialObjects[_activeIndex];
                panel.SetActive(true);
                _activePanelView = panel.GetComponent<TutorialPanelView>();
                _activePanelView?.ResetMarkers();
            }
        }

        RefreshCompletionText();
    }

    private void RefreshCompletionText()
    {
        var step = tutorialManager.CurrentStep;
        if (completionText == null) return;

        completionText.text = step != null
            ? $"{step.completionCount} / {step.requiredCompletions}"
            : "";
    }

    private void HandleProgress(TutorialStep step, int sequenceProgress)
    {
        _activePanelView?.SetHighlightIndex(sequenceProgress);
        RefreshCompletionText();
    }

    private void HandleStepCompleted(TutorialStep step)
    {
        RefreshActivePanel(); // swaps to new panel, resets markers on the new one
    }

    private void HandleFinished()
    {
        if (_activeIndex >= 0 && _activeIndex < tutorialObjects.Count && tutorialObjects[_activeIndex] != null)
            tutorialObjects[_activeIndex].SetActive(false);

        if (completionText != null)
            completionText.text = "Tutorial Complete!";
    }
}