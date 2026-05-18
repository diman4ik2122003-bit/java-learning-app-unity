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

    private LevelGameManager levelManager;
    //private UniversalLevelManager universalLevelManager;
    private ScaleController scaleController;

    private void OnEnable()
    {
        levelManager = FindFirstObjectByType<LevelGameManager>();
        //universalLevelManager = FindFirstObjectByType<UniversalLevelManager>();
        scaleController = FindFirstObjectByType<ScaleController>();

        if (levelManager == null) 
            Debug.LogError("[CodeEditorButtonBridge] NO LEVEL MANAGER FOUND!");

        if (runButton != null)
        {
            runButton.onClick.RemoveAllListeners();
            runButton.onClick.AddListener(OnRunClicked);
        }
        else Debug.LogError("[CodeEditorButtonBridge] Run Button field is EMPTY!");

        if (resetCodeButton != null)
        {
            resetCodeButton.onClick.RemoveAllListeners();
            resetCodeButton.onClick.AddListener(OnResetClicked);
        }

        if (scaleController != null)
        {
            if (plusScaleButton != null) plusScaleButton.onClick.AddListener(scaleController.OnPlusScale);
            if (minusScaleButton != null) minusScaleButton.onClick.AddListener(scaleController.OnMinusScale);
            if (resetScaleButton != null) resetScaleButton.onClick.AddListener(scaleController.OnResetScale);
        }
    }

    private void OnRunClicked()
    {
        if (levelManager != null)
            levelManager.OnRunCode();
        else
            Debug.LogError("[CodeEditorButtonBridge] LevelManager not found in scene!");
    }

    private void OnResetClicked()
    {
        if (levelManager != null)
            levelManager.OnResetLevel();
        else
            Debug.LogError("[CodeEditorButtonBridge] LevelManager not found!");
    }
}
