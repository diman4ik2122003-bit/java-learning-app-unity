using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text taskTitle;
    public TMP_Text taskDescription;
    public CodeEditorUIToolkit codeEditorUIToolkit;
    
    [Header("Hint UI")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    public UnityEngine.UI.Button closeHintButton;
    public UnityEngine.UI.Button showHintButton; // Кнопка внизу экрана "Показать подсказку"
    public UnityEngine.UI.Button useSolutionButton; // Кнопка "Использовать решение"

    [Header("Game References")]
    public PlayerController player;
    public Transform goalTransform;

    [Header("Execution Mode")]
    public bool useJavaServer = true;

    [Header("UI Panels")]
    public GameObject victoryPanel;

    [Header("Level Progression")]
    public LevelData[] allLevels;
    private int currentLevelIndex = 0;
    
    // ⭐ Система провалов и подсказок
    private int failedAttempts = 0;
    private bool levelCompleted = false;
    private LevelData currentLevel;
    private int currentHintIndex = 0; // Текущий уровень подсказки (0-3)

    void Start()
    {
        // Загружаем уровень
        if (LevelSelectionManager.SelectedLevel != null)
        {
            Debug.Log("[LevelManager] Загружаем уровень из roadmap: " + LevelSelectionManager.SelectedLevel.levelId);
            LoadLevelDirectly(LevelSelectionManager.SelectedLevel);
            LevelSelectionManager.SelectedLevel = null;
        }
        else if (allLevels != null && allLevels.Length > 0)
        {
            Debug.Log("[LevelManager] Загружаем первый уровень из массива");
            LoadLevelByIndex(0);
        }
        else
        {
            Debug.LogError("[LevelManager] Нет уровней для загрузки!");
        }
        
        HideHintUI();
    }

    void LoadLevelDirectly(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[LevelManager] LevelData is null!");
            return;
        }

        currentLevel = level;
        currentLevelIndex = -1;
        
        // Сброс системы подсказок
        failedAttempts = 0;
        currentHintIndex = 0;
        levelCompleted = false;
        HideHintUI();

        // UI
        taskTitle.text = $"{level.groupName} - {level.levelName}";
        taskDescription.text = level.description;

        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.SetCode(level.starterCode);
            codeEditorUIToolkit.ClearConsole();
        }

        if (player != null)
        {
            Debug.Log($"[LevelManager] Устанавливаем позицию игрока: {level.playerStartPosition}");
            player.SetStartPosition(level.playerStartPosition);
        }

        if (goalTransform != null)
        {
            Debug.Log($"[LevelManager] Устанавливаем позицию цели: {level.goalPosition}");
            goalTransform.position = level.goalPosition;
        }

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        Debug.Log($"[LevelManager] ✅ Загружен уровень: {level.levelId}");
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
        
        Debug.Log($"[LevelManager] Загружаем уровень по индексу {index}: {level.levelId}");
        LoadLevelDirectly(level);
    }

    public void OnRunCode()
    {
        Debug.Log("[LevelManager] ⭐ OnRunCode() вызван");

        if (codeEditorUIToolkit == null)
        {
            Debug.LogError("[LevelManager] CodeEditorUIToolkit не назначен!");
            return;
        }

        player.ResetState();

        if (useJavaServer)
        {
            JavaCodeExecutor executor = FindObjectOfType<JavaCodeExecutor>();
            if (executor != null)
            {
                executor.ExecuteCode();
            }
            else
            {
                Debug.LogError("[LevelManager] JavaCodeExecutor не найден!");
            }
        }
        else
        {
            if (CodeExecutor.Instance != null)
            {
                CodeExecutor.Instance.Execute(codeEditorUIToolkit.GetCode(), player);
            }
        }
    }

    public void OnExecutionFinished()
    {
        Debug.Log("[LevelManager] ⭐ Выполнение завершено. Проверяем успех...");
        Invoke(nameof(CheckAfterExecution), 0.3f);
    }
    
    void CheckAfterExecution()
    {
        if (!levelCompleted)
        {
            OnLevelFailed();
        }
    }

    void OnLevelFailed()
    {
        failedAttempts++;
        
        if (currentLevel == null)
        {
            Debug.LogError("[LevelManager] currentLevel is null!");
            return;
        }
        
        Debug.Log($"[LevelManager] ❌ ПРОВАЛ! Попытка {failedAttempts}");
        
        // ⭐ В консоль выводим только счётчик попыток
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog($"❌ Попытка {failedAttempts}. Попробуй ещё раз!");
        }
        
        // ⭐ ПРОГРЕССИВНЫЕ ПОДСКАЗКИ
        int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;
        
        if (failedAttempts >= attemptsPerHint * 4)
        {
            // 12+ попыток → КНОПКА "Использовать решение"
            currentHintIndex = 4;
            ShowSolutionButton();
            if (codeEditorUIToolkit != null)
                codeEditorUIToolkit.AddConsoleLog("💡 Кнопка 'Решение' доступна!");
        }
        else if (failedAttempts >= attemptsPerHint * 3)
        {
            // 9+ попыток → ПОДСКАЗКА 3
            currentHintIndex = 3;
            EnableHintButton();
        }
        else if (failedAttempts >= attemptsPerHint * 2)
        {
            // 6+ попыток → ПОДСКАЗКА 2
            currentHintIndex = 2;
            EnableHintButton();
        }
        else if (failedAttempts >= attemptsPerHint)
        {
            // 3+ попытки → ПОДСКАЗКА 1
            currentHintIndex = 1;
            EnableHintButton();
        }
    }

    // ⭐ Включаем кнопку "Показать подсказку"
    void EnableHintButton()
    {
        if (showHintButton != null && !showHintButton.gameObject.activeSelf)
        {
            showHintButton.gameObject.SetActive(true);
            
            if (codeEditorUIToolkit != null)
            {
                codeEditorUIToolkit.AddConsoleLog("💡 Подсказка доступна! Нажми кнопку 'Подсказка'");
            }
            
            Debug.Log($"[LevelManager] 💡 Подсказка {currentHintIndex} доступна");
        }
    }

    void ShowSolutionButton()
    {
        if (useSolutionButton != null)
        {
            useSolutionButton.gameObject.SetActive(true);
        }
    }

    // ⭐ КНОПКА "Показать подсказку" (открывает панель)
    public void OnShowHint()
    {
        if (currentLevel == null || hintPanel == null || hintText == null)
        {
            Debug.LogError("[LevelManager] Hint UI не настроен!");
            return;
        }
        
        string hintMessage = GetCurrentHint();
        
        if (!string.IsNullOrEmpty(hintMessage))
        {
            hintText.text = hintMessage;
            hintPanel.SetActive(true);
            
            Debug.Log($"[LevelManager] 💡 Показана подсказка {currentHintIndex}: {hintMessage}");
        }
        else
        {
            Debug.LogWarning("[LevelManager] Подсказка пуста!");
        }
    }

    // ⭐ Получить текущую подсказку
    string GetCurrentHint()
    {
        if (currentLevel == null) return "";
        
        switch (currentHintIndex)
        {
            case 1: return currentLevel.hint1;
            case 2: return currentLevel.hint2;
            case 3: return currentLevel.hint3;
            default: return currentLevel.hint; // fallback
        }
    }

    // ⭐ КНОПКА "Использовать решение"
    public void OnUseSolution()
    {
        if (currentLevel == null)
        {
            Debug.LogError("[LevelManager] currentLevel is null!");
            return;
        }
        
        if (codeEditorUIToolkit != null && !string.IsNullOrEmpty(currentLevel.solutionCode))
        {
            codeEditorUIToolkit.SetCode(currentLevel.solutionCode);
            codeEditorUIToolkit.AddConsoleLog("💡 Загружено правильное решение. Нажми Run!");
            
            Debug.Log("[LevelManager] Загружено решение уровня");
        }
        
        // Скрываем кнопку после использования
        if (useSolutionButton != null)
            useSolutionButton.gameObject.SetActive(false);
    }

    // ⭐ КНОПКА "Закрыть" в HintPanel
    public void OnCloseHint()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
            Debug.Log("[LevelManager] Панель подсказок закрыта");
        }
    }
    
    void HideHintUI()
    {
        if (showHintButton != null)
            showHintButton.gameObject.SetActive(false);
        if (useSolutionButton != null)
            useSolutionButton.gameObject.SetActive(false);
        if (hintPanel != null)
            hintPanel.SetActive(false);
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
            // ⭐ НЕ сбрасываем failedAttempts и currentHintIndex
            // Подсказки остаются доступными
            if (codeEditorUIToolkit != null)
            {
                codeEditorUIToolkit.SetCode(currentLevel.starterCode);
                codeEditorUIToolkit.ClearConsole();
            }
            
            player.SetStartPosition(currentLevel.playerStartPosition);
            levelCompleted = false;
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
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog("🎉 Уровень пройден!");
            
            if (failedAttempts == 0)
                codeEditorUIToolkit.AddConsoleLog("⭐ Идеально! Решено с первой попытки!");
            else if (failedAttempts <= 2)
                codeEditorUIToolkit.AddConsoleLog($"✨ Отлично! Попыток: {failedAttempts + 1}");
            else
                codeEditorUIToolkit.AddConsoleLog($"📊 Попыток: {failedAttempts + 1}");
        }
        
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    public void OnNextLevel()
    {
        Debug.Log("[LevelManager] Загрузка следующего уровня...");

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (currentLevelIndex >= 0)
        {
            LoadLevelByIndex(currentLevelIndex + 1);
        }
        else
        {
            Debug.LogWarning("[LevelManager] Следующий уровень недоступен");
        }
    }
}
