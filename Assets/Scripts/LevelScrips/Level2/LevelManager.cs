// Assets/Scripts/LevelScripts/LevelManager.cs
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
    public LevelData[] allLevels; // Fallback если уровень не выбран из roadmap
    private int currentLevelIndex = 0;
    private bool levelCompleted = false;
    private LevelData currentLevel; // Текущий загруженный уровень

    void Start()
    {
        // ⭐ Проверяем был ли выбран уровень из roadmap
        if (LevelSelectionManager.SelectedLevel != null)
        {
            LoadLevelDirectly(LevelSelectionManager.SelectedLevel);
            LevelSelectionManager.SelectedLevel = null; // Очищаем после загрузки
        }
        else if (allLevels != null && allLevels.Length > 0)
        {
            // Fallback: загружаем первый уровень из массива
            LoadLevelByIndex(0);
        }
        else
        {
            Debug.LogError("[LevelManager] Нет уровней для загрузки!");
        }
    }

    // ⭐ Загружает конкретный LevelData напрямую
    void LoadLevelDirectly(LevelData level)
    {
        if (!level)
        {
            Debug.LogError("[LevelManager] LevelData is null!");
            return;
        }

        currentLevel = level;
        currentLevelIndex = -1; // Указываем что уровень загружен не из массива

        taskTitle.text = level.levelName;
        taskDescription.text = level.description;

        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.SetCode(level.starterCode);
        }

        player.transform.position = level.playerStartPosition;
        goalTransform.position = level.goalPosition;

        player.ResetState();
        levelCompleted = false;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        Debug.Log("[LevelManager] Загружен уровень: " + level.levelId);
    }

    void LoadLevelByIndex(int index)
    {
        if (index < 0 || index >= allLevels.Length)
        {
            Debug.LogWarning("[LevelManager] Нет больше уровней!");
            return;
        }

        currentLevelIndex = index;
        LoadLevelDirectly(allLevels[index]);
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
        
        if (currentLevel != null)
        {
            LoadLevelDirectly(currentLevel);
        }
        else if (currentLevelIndex >= 0 && currentLevelIndex < allLevels.Length)
        {
            LoadLevelByIndex(currentLevelIndex);
        }
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

        // Если уровень был загружен из roadmap - переход на следующий в массиве
        if (currentLevelIndex >= 0)
        {
            LoadLevelByIndex(currentLevelIndex + 1);
        }
        else
        {
            Debug.LogWarning("[LevelManager] Следующий уровень недоступен (уровень загружен из roadmap)");
        }
    }
}
