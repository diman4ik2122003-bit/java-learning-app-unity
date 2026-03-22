using UnityEngine;
using TMPro;
using System.Collections;
using System; // ⭐ ДОБАВЬ ЭТО

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
    public LevelData[] allLevels;
    private int currentLevelIndex = 0;
    
    public VictoryPanelUI victoryPanelUI;

    // ⭐ Система провалов и подсказок
    private int failedAttempts = 0;
    private int hintsUsedCount = 0;
    private int currentHintIndex = 0;
    private bool levelCompleted = false;
    private LevelData currentLevel;
    
    // ⭐ Система отслеживания прогресса
    private float levelStartTime = 0f;

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
        
        // ⭐ Сброс всех счётчиков
        failedAttempts = 0;
        hintsUsedCount = 0;
        currentHintIndex = 0;
        levelCompleted = false;
        levelStartTime = Time.time;
        
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
            JavaCodeExecutor executor = FindFirstObjectByType<JavaCodeExecutor>();
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
            else
            {
                Debug.LogError("[LevelManager] CodeExecutor.Instance не найден!");
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
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog($"❌ Попытка {failedAttempts}. Попробуй ещё раз!");
        }
        
        // ⭐ ПРОГРЕССИВНЫЕ ПОДСКАЗКИ
        int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;
        
        if (failedAttempts >= attemptsPerHint * 4)
        {
            currentHintIndex = 4;
            ShowSolutionButton();
            if (codeEditorUIToolkit != null)
                codeEditorUIToolkit.AddConsoleLog("💡 Кнопка 'Решение' доступна!");
        }
        else if (failedAttempts >= attemptsPerHint * 3)
        {
            currentHintIndex = 3;
            EnableHintButton();
        }
        else if (failedAttempts >= attemptsPerHint * 2)
        {
            currentHintIndex = 2;
            EnableHintButton();
        }
        else if (failedAttempts >= attemptsPerHint)
        {
            currentHintIndex = 1;
            EnableHintButton();
        }
    }

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

    string GetCurrentHint()
    {
        if (currentLevel == null) return "";
        
        switch (currentHintIndex)
        {
            case 1: return currentLevel.hint1;
            case 2: return currentLevel.hint2;
            case 3: return currentLevel.hint3;
            default: return currentLevel.hint;
        }
    }

    public void OnUseSolution()
    {
        if (currentLevel == null)
        {
            Debug.LogError("[LevelManager] currentLevel is null!");
            return;
        }
        
        hintsUsedCount++;
        
        if (codeEditorUIToolkit != null && !string.IsNullOrEmpty(currentLevel.solutionCode))
        {
            codeEditorUIToolkit.SetCode(currentLevel.solutionCode);
            codeEditorUIToolkit.AddConsoleLog("💡 Загружено правильное решение. Нажми Run!");
            
            Debug.Log("[LevelManager] Загружено решение уровня");
        }
        
        if (useSolutionButton != null)
            useSolutionButton.gameObject.SetActive(false);
    }

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
        if (levelCompleted) return;
        
        levelCompleted = true;
        
        Debug.Log("[LevelManager] 🎉 Уровень пройден!");

        if (CodeExecutor.Instance != null)
        {
            CodeExecutor.Instance.StopExecution();
        }
        
        // ⭐ ВЫЧИСЛЯЕМ СТАТИСТИКУ
        int completionTime = Mathf.RoundToInt(Time.time - levelStartTime);
        int stars = CalculateStars(failedAttempts, hintsUsedCount, completionTime);
        int codeLines = CountCodeLines(codeEditorUIToolkit.GetCode());
        int attemptsTotal = failedAttempts + 1;

        Debug.Log($"[LevelManager] Статистика: time={completionTime}s, attempts={attemptsTotal}, stars={stars}");
        
        if (victoryPanelUI != null)
        {
            victoryPanelUI.SetStats(completionTime, attemptsTotal, stars, 0);
        }

        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog("🎉 Уровень пройден!");
            codeEditorUIToolkit.AddConsoleLog($"⭐ Звёзд: {stars}/3");
            codeEditorUIToolkit.AddConsoleLog($"⏱️ Время: {completionTime}с");
            codeEditorUIToolkit.AddConsoleLog($"📊 Попыток: {failedAttempts + 1}");
            
            if (failedAttempts == 0)
                codeEditorUIToolkit.AddConsoleLog("🏆 Идеально! Решено с первой попытки!");
            else if (failedAttempts <= 2)
                codeEditorUIToolkit.AddConsoleLog($"✨ Отлично!");
        }
        
        // ⭐ ДИАГНОСТИКА
        Debug.Log("=== ДИАГНОСТИКА ПРОГРЕССА ===");
        Debug.Log($"ProgressAPI.Instance: {(ProgressAPIService.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Token: {PlayerPrefs.GetString("authToken", "NO_TOKEN")}");
        Debug.Log($"Challenge ID: {currentLevel?.levelId}");
        Debug.Log("==============================");

        // ⭐ ПОКАЗЫВАЕМ ПАНЕЛЬ С ДИАГНОСТИКОЙ
        if (victoryPanel != null)
        {
            Debug.Log("[LevelManager] Активируем victoryPanel");
            victoryPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[LevelManager] victoryPanel == null!");
        }
        
        if (victoryPanelUI != null)
        {
            Debug.Log($"[LevelManager] Вызываем SetStats({completionTime}, {attemptsTotal}, {stars}, 0)");
            victoryPanelUI.SetStats(completionTime, attemptsTotal, stars, 0);
        }
        else
        {
            Debug.LogError("[LevelManager] victoryPanelUI == null!");
        }
        
        // ⭐ СОХРАНЯЕМ НА СЕРВЕРЕ (ОДИН РАЗ)
        if (currentLevel != null)
        {
            SaveProgressToBackend(
                currentLevel.levelId,
                stars,
                completionTime,
                failedAttempts,
                hintsUsedCount,
                codeLines
            );
        }
        
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    int CalculateStars(int attempts, int hints, int time)
    {
        int stars = 3;
        
        if (attempts > 5) stars = 1;
        else if (attempts > 2) stars = 2;
        
        if (hints > 0 && stars > 1) stars--;
        
        if (time > 300 && stars > 1) stars--;
        
        return Mathf.Max(1, stars);
    }

    int CountCodeLines(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0;
        
        string[] lines = code.Split('\n');
        int count = 0;
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//"))
            {
                count++;
            }
        }
        
        return count;
    }

    void SaveProgressToBackend(
        string challengeId,
        int stars,
        int completionTime,
        int failedAttempts,
        int hintsUsed,
        int codeLines)
    {
        Debug.Log($"[LevelManager] Сохранение прогресса на сервер...");
        Debug.Log($"Challenge: {challengeId}, Stars: {stars}, Time: {completionTime}s");
        
        StartCoroutine(ProgressAPIService.Instance.SaveLevelCompletion(
            challengeId,
            stars,
            completionTime,
            failedAttempts,
            hintsUsed,
            codeLines,
            OnProgressSaved, // ⭐ БЕЗ скобок - передаём метод как делегат
            OnProgressSaveError
        ));
    }

    // ⭐ ПРАВИЛЬНАЯ СИГНАТУРА (с полным путём)
    void OnProgressSaved(ProgressAPIService.LevelCompletionResponse response)
    {
        Debug.Log($"[LevelManager] ✅ Прогресс сохранён!");
        Debug.Log($"XP получено: +{response.data.xpGained}");
        Debug.Log($"Уровень: {response.data.stats.level}");
        int xpGained = response.data.xpGained;
        
        if (victoryPanelUI != null)
        {
            int completionTime = Mathf.RoundToInt(Time.time - levelStartTime);
            int attemptsTotal = failedAttempts + 1;
            int stars = CalculateStars(failedAttempts, hintsUsedCount, completionTime);

            victoryPanelUI.SetStats(completionTime, attemptsTotal, stars, xpGained);
        }

        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog($"💰 +{response.data.xpGained} XP");
            codeEditorUIToolkit.AddConsoleLog($"🎯 Уровень: {response.data.stats.level}");
        }
        
        if (response.data.achievements != null && response.data.achievements.Length > 0)
        {
            foreach (var achievement in response.data.achievements)
            {
                if (achievement.isNew)
                {
                    Debug.Log($"🏆 Новое достижение: {achievement.name}");
                    if (codeEditorUIToolkit != null)
                    {
                        codeEditorUIToolkit.AddConsoleLog($"🏆 {achievement.name}");
                    }
                }
            }
        }
    }

    void OnProgressSaveError(string error)
    {
        Debug.LogWarning($"[LevelManager] ⚠️ Не удалось сохранить прогресс: {error}");
        
        if (codeEditorUIToolkit != null)
        {
            codeEditorUIToolkit.AddConsoleLog("⚠️ Прогресс не сохранён (offline)");
        }
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
