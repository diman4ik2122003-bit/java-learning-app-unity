using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text taskTitle;
    public TMP_Text taskDescription;
    public CodeEditor codeEditor;
    
    [Header("Hint UI")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    public UnityEngine.UI.Button closeHintButton;
    public UnityEngine.UI.Button showHintButton;
    public UnityEngine.UI.Button useSolutionButton;

    [Header("UI Panels")]
    public GameObject victoryPanel;
    public VictoryPanelUI victoryPanelUI;
    
    public void SetTaskInfo(string title, string description)
    {
        if (taskTitle != null) taskTitle.text = title;
        if (taskDescription != null) taskDescription.text = description;
    }

    public void ShowHint(string hintMessage)
    {
        if (hintPanel != null && hintText != null)
        {
            hintText.text = hintMessage;
            hintPanel.SetActive(true);
        }
    }

    public void HideHintUI()
    {
        if (showHintButton != null) showHintButton.gameObject.SetActive(false);
        if (useSolutionButton != null) useSolutionButton.gameObject.SetActive(false);
        if (hintPanel != null) hintPanel.SetActive(false);
    }
    
    public void OnCloseHint()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    public void EnableHintButton()
    {
        if (showHintButton != null) showHintButton.gameObject.SetActive(true);
    }

    public void ShowSolutionButton()
    {
        if (useSolutionButton != null) useSolutionButton.gameObject.SetActive(true);
    }
}
