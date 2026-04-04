using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class CodeEditorButtonBridge : MonoBehaviour
{
    [Header("Main Buttons")]
    public Button runButton;
    public Button resetCodeButton;

    [Header("Scaling Buttons")]
    public Button plusScaleButton;
    public Button minusScaleButton;
    public Button resetScaleButton;

    private LevelManager levelManager;
    private UniversalLevelManager universalLevelManager;
    private ScaleController scaleController;

    private void OnEnable()
    {
        Debug.Log("[CodeEditorButtonBridge] OnEnable: Searching for LevelManager...");
        levelManager = FindFirstObjectByType<LevelManager>();
        universalLevelManager = FindFirstObjectByType<UniversalLevelManager>();
        scaleController = FindFirstObjectByType<ScaleController>();

        if (levelManager != null) Debug.Log("[CodeEditorButtonBridge] Found LevelManager!");
        if (universalLevelManager != null) Debug.Log("[CodeEditorButtonBridge] Found UniversalLevelManager!");

        if (levelManager == null && universalLevelManager == null) 
            Debug.LogError("[CodeEditorButtonBridge] NO LEVEL MANAGER FOUND!");

        if (runButton != null)
        {
            runButton.onClick.RemoveAllListeners();
            runButton.onClick.AddListener(OnRunClicked);
            Debug.Log("[CodeEditorButtonBridge] Bound Run Button");
        }
        else Debug.LogError("[CodeEditorButtonBridge] Run Button field is EMPTY!");

        if (resetCodeButton != null)
        {
            resetCodeButton.onClick.RemoveAllListeners();
            resetCodeButton.onClick.AddListener(OnResetClicked);
            Debug.Log("[CodeEditorButtonBridge] Bound Reset Button");
        }

        if (scaleController != null)
        {
            if (plusScaleButton != null) plusScaleButton.onClick.AddListener(scaleController.OnPlusScale);
            if (minusScaleButton != null) minusScaleButton.onClick.AddListener(scaleController.OnMinusScale);
            if (resetScaleButton != null) resetScaleButton.onClick.AddListener(scaleController.OnResetScale);
            Debug.Log("[CodeEditorButtonBridge] Bound Scale Buttons");
        }
    }

    private void OnRunClicked()
    {
        Debug.Log("[CodeEditorButtonBridge] RUN Button Clicked");
        if (levelManager != null)
            levelManager.OnRunCode();
        else
            Debug.LogError("[CodeEditorButtonBridge] LevelManager not found in scene!");
    }

    private void OnResetClicked()
    {
        Debug.Log("[CodeEditorButtonBridge] RESET Button Clicked");
        if (levelManager != null)
            levelManager.OnResetLevel();
        else if (universalLevelManager != null)
            universalLevelManager.RestartLevel();
        else
            Debug.LogError("[CodeEditorButtonBridge] LevelManager not found!");
    }
}
