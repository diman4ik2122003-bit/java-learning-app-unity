using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text taskTitle;
    public TMP_Text taskDescription;
    public CodeEditorUIToolkit codeEditorUIToolkit;

    [Header("Game References")]
    public PlayerController player;
    public Transform goalTransform;

    [Header("UI Panels")]
    public GameObject victoryPanel;

    [Header("Level Progression")]
    public LevelData[] allLevels;
    private int currentLevelIndex = 0;
    private bool levelCompleted = false;

    void Start()
    {
        if (allLevels != null && allLevels.Length > 0)
            LoadLevelByIndex(0);
    }

    void LoadLevelByIndex(int index)
    {
        if (index < 0 || index >= allLevels.Length)
        {
            Debug.LogWarning("[LevelManager] Нет больше уровней!");
            return;
        }
        
        currentLevelIndex = index;
        LevelData level = allLevels[index];
        
        // Используем правильные поля из LevelData
        taskTitle.text = level.levelName; // было level.title
        taskDescription.text = level.description;
        
        // Устанавливаем стартовый код
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.SetCode(level.starterCode);
        }
        
        player.transform.position = level.playerStartPosition;
        goalTransform.position = level.goalPosition;
        
        player.ResetState();
        levelCompleted = false;
    }

    public void OnRunCode()
    {
        if (codeEditorUIToolkit == null)
        {
            Debug.LogError("[LevelManager] CodeEditorUIToolkit не назначен!");
            return;
        }

        string userCode = codeEditorUIToolkit.GetCode();
        Debug.Log("[LevelManager] Запуск кода:\n" + userCode);
        
        player.ResetState();
        
        if (CodeExecutor.Instance != null)
        {
            CodeExecutor.Instance.Execute(userCode, player);
        }
        else
        {
            Debug.LogError("[LevelManager] CodeExecutor.Instance не найден!");
        }
    }

    public void OnResetLevel()
    {
        Debug.Log("[LevelManager] Сброс уровня");
        
        if (CodeExecutor.Instance != null)
        {
            CodeExecutor.Instance.StopExecution();
        }
        
        player.ResetState();
        LoadLevelByIndex(currentLevelIndex);
    }

    void Update()
    {
        if (!levelCompleted && player != null && goalTransform != null)
        {
            float distance = Vector2.Distance(player.transform.position, goalTransform.position);
            if (distance < 0.5f)
            {
                OnLevelCompleted();
            }
        }
    }

    void OnLevelCompleted()
    {
        levelCompleted = true;
        Debug.Log("[LevelManager] 🎉 Уровень пройден!");
        
        if (CodeExecutor.Instance != null)
        {
            CodeExecutor.Instance.StopExecution();
        }
        
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    public void OnNextLevel()
    {
        Debug.Log("[LevelManager] Загрузка следующего уровня...");
        
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        
        LoadLevelByIndex(currentLevelIndex + 1);
    }
}
