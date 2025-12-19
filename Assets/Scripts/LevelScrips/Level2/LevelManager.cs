// Assets/Scripts/LevelScripts/LevelManager.cs
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
    public UnityEngine.UI.Button showHintButton;
    public UnityEngine.UI.Button useSolutionButton;

    [Header("Game References")]
    public PlayerController player;
    public Transform goalTransform;

    [Header("Execution Mode")]
    public bool useJavaServer = true;

    [Header("UI Panels")]
    public GameObject victoryPanel;

    [Header("Level Progression")]
    public LevelData[] allLevels; // Fallback если уровень не выбран из roadmap
    private int currentLevelIndex = 0;
    
    // ⭐ Система провалов и подсказок
    private int failedAttempts = 0;
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
        
        HideHintUI();
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
        LevelData level = allLevels[index];
        
        // Сброс всего
        failedAttempts = 0;
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

        // Позиции
        player.SetStartPosition(level.playerStartPosition);
        
        if (goalTransform != null)
        {
            goalTransform.position = level.goalPosition;
        }
        
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
            
        Debug.Log($"[LevelManager] Загружен уровень: {level.levelId}");
        LoadLevelDirectly(allLevels[index]);
    }

    public void OnRunCode()
    {
        Debug.Log("[LevelManager] ⭐ OnRunCode() вызван");

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
                CodeExecutor.Instance.Execute(userCode, player);
            }
            else
            {
                Debug.LogError("[LevelManager] CodeExecutor.Instance не найден!");
            }
        }
    }

    // ⭐ ВЫЗЫВАЕТСЯ ИЗ JavaCodeExecutor ПОСЛЕ ВЫПОЛНЕНИЯ КОДА
    public void OnExecutionFinished()
    {
        Debug.Log("[LevelManager] ⭐ Выполнение завершено. Проверяем успех...");
        
        // Даём 0.3 секунды на завершение анимаций
        Invoke(nameof(CheckAfterExecution), 0.3f);
    }
    
    void CheckAfterExecution()
    {
        if (!levelCompleted)
        {
            OnLevelFailed();
        }
    }

    // ⭐ Провал уровня
    void OnLevelFailed()
    {
        failedAttempts++;
        
        LevelData currentLevel = allLevels[currentLevelIndex];
        Debug.Log($"[LevelManager] ❌ ПРОВАЛ! Попытка {failedAttempts}");
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog($"❌ Попытка {failedAttempts}. Попробуй ещё раз!");
        }
        
        // ⭐ ПРОГРЕССИВНЫЕ ПОДСКАЗКИ
        int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;
        
        if (failedAttempts >= attemptsPerHint * 4)
        {
            // 12+ попыток → ПОЛНОЕ РЕШЕНИЕ ДОСТУПНО
            ShowSolutionButton();
            if (codeEditorUIToolkit != null)
                codeEditorUIToolkit.AddConsoleLog("💡 Нужна помощь? Используй кнопку 'Решение'");
        }
        else if (failedAttempts >= attemptsPerHint * 3)
        {
            // 9+ попыток → ПОДСКАЗКА 3
            ShowHint(currentLevel.hint3, 3);
        }
        else if (failedAttempts >= attemptsPerHint * 2)
        {
            // 6+ попыток → ПОДСКАЗКА 2
            ShowHint(currentLevel.hint2, 2);
        }
        else if (failedAttempts >= attemptsPerHint)
        {
            // 3+ попытки → ПОДСКАЗКА 1
            ShowHint(currentLevel.hint1, 1);
        }
    }

    void ShowHint(string hintMessage, int hintLevel)
    {
        if (string.IsNullOrEmpty(hintMessage)) return;
        
        Debug.Log($"[LevelManager] 💡 Подсказка {hintLevel}: {hintMessage}");
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog($"💡 Подсказка {hintLevel}: {hintMessage}");
        }
        
        // Показываем кнопку "Показать подсказку"
        if (showHintButton != null)
        {
            showHintButton.gameObject.SetActive(true);
        }
    }

    void ShowSolutionButton()
    {
        if (useSolutionButton != null)
        {
            useSolutionButton.gameObject.SetActive(true);
        }
    }

    // ⭐ Кнопка "Показать подсказку"
    public void OnShowHint()
    {
        if (hintPanel != null && hintText != null)
        {
            LevelData currentLevel = allLevels[currentLevelIndex];
            
            // Показываем последнюю доступную подсказку
            string hint = "";
            int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;
            
            if (failedAttempts >= attemptsPerHint * 3)
                hint = currentLevel.hint3;
            else if (failedAttempts >= attemptsPerHint * 2)
                hint = currentLevel.hint2;
            else if (failedAttempts >= attemptsPerHint)
                hint = currentLevel.hint1;
            else
                hint = currentLevel.hint; // fallback
            
            hintText.text = hint;
            hintPanel.SetActive(true);
        }
    }

    // ⭐ Кнопка "Использовать решение"
    public void OnUseSolution()
    {
        LevelData currentLevel = allLevels[currentLevelIndex];
        
        if (codeEditorUIToolkit != null && !string.IsNullOrEmpty(currentLevel.solutionCode))
        {
            codeEditorUIToolkit.SetCode(currentLevel.solutionCode);
            codeEditorUIToolkit.AddConsoleLog("💡 Загружено правильное решение. Нажми Run!");
        }
        
        if (useSolutionButton != null)
            useSolutionButton.gameObject.SetActive(false);
    }

    // ⭐ Закрыть панель подсказок
    public void OnCloseHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
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
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog("🎉 Уровень пройден!");
            
            // Статистика
            if (failedAttempts == 0)
            {
                codeEditorUIToolkit.AddConsoleLog("⭐ Идеально! Решено с первой попытки!");
            }
            else if (failedAttempts <= 2)
            {
                codeEditorUIToolkit.AddConsoleLog($"✨ Отлично! Попыток: {failedAttempts + 1}");
            }
            else
            {
                codeEditorUIToolkit.AddConsoleLog($"📊 Попыток: {failedAttempts + 1}");
            }
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
