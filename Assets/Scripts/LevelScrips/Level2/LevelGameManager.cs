using UnityEngine;
using System.Collections;
using System;

public class LevelGameManager : MonoBehaviour
{
    public static LevelGameManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Ссылка на оригинальный LevelManager (который теперь отвечает только за UI)")]
    public LevelManager uiManager;

    [Header("Game References")]
    public PlayerController player;
    public Transform goalTransform;

    [Header("Execution Mode")]
    public bool useJavaServer = true;

    [Header("Level Configuration")]
    [Tooltip("Данные текущего уровня")]
    public LevelData currentLevel;

    [Header("Runtime Status")]
    public bool progressMadeThisRun = false;

    // Системные счетчики
    private int failedAttempts = 0;
    private int hintsUsedCount = 0;
    private int currentHintIndex = 0;
    private bool levelCompleted = false;
    private float levelStartTime = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<LevelManager>();

        if (LevelSelectionManager.SelectedLevel != null)
        {
            Debug.Log("[LevelGameManager] Загружаем уровень из roadmap: " + LevelSelectionManager.SelectedLevel.levelId);
            LoadLevelDirectly(LevelSelectionManager.SelectedLevel);
            LevelSelectionManager.SelectedLevel = null;
        }
        else if (currentLevel != null)
        {
            Debug.Log("[LevelGameManager] Загружаем выделенный currentLevel");
            LoadLevelDirectly(currentLevel);
        }
        else
        {
            Debug.LogError("[LevelGameManager] Нет уровней для загрузки!");
        }
    }

    void LoadLevelDirectly(LevelData level)
    {
        if (level == null) return;

        currentLevel = level;
        failedAttempts = 0;
        hintsUsedCount = 0;
        currentHintIndex = 0;
        levelCompleted = false;
        levelStartTime = Time.time;

        if (uiManager != null)
        {
            uiManager.HideHintUI();
            uiManager.SetTaskInfo($"{level.groupName} - {level.levelName}", level.description);

            if (uiManager.codeEditor != null)
            {
                uiManager.codeEditor.SetCode(level.starterCode);
                uiManager.codeEditor.ClearConsole();
            }

            if (uiManager.victoryPanel != null)
                uiManager.victoryPanel.SetActive(false);
        }

        if (player != null)
        {
            player.SetStartPosition(level.playerStartPosition);
        }

        if (goalTransform != null)
        {
            goalTransform.position = level.goalPosition;
        }

        Debug.Log($"[LevelGameManager] ✅ Загружен уровень: {level.levelId}");
    }

    public void OnRunCode()
    {
        Debug.Log("[LevelGameManager] ⭐ OnRunCode() вызван");

        if (uiManager == null)
        {
            Debug.LogError("[LevelGameManager] uiManager is null! Please assign LevelManager in the Inspector.");
            return;
        }
        if (uiManager.codeEditor == null)
        {
            Debug.LogWarning("[LevelGameManager] uiManager.codeEditor is null! RunCode will proceed, but local logic may fail.");
        }

        progressMadeThisRun = false;

        ElevatorLevelController elc = FindFirstObjectByType<ElevatorLevelController>();
        if (elc == null && player != null)
        {
            player.ResetState();
        }

        if (useJavaServer)
        {
            JavaCodeExecutor executor = FindFirstObjectByType<JavaCodeExecutor>();
            if (executor != null)
            {
                executor.ExecuteCode();
            }
            else
            {
                Debug.LogError("[LevelGameManager] JavaCodeExecutor is missing in the scene!");
                if (uiManager != null && uiManager.codeEditor != null)
                    uiManager.codeEditor.AddConsoleLog("❌ Ошибка: В сцене отсутствует JavaCodeExecutor!", true);
            }
        }
        else
        {
            if (CodeExecutor.Instance != null)
            {
                CodeExecutor.Instance.Execute(uiManager?.codeEditor?.GetCode(), player);
            }
            else
            {
                Debug.LogError("[LevelGameManager] Local CodeExecutor.Instance is null!");
                if (uiManager != null && uiManager.codeEditor != null)
                    uiManager.codeEditor.AddConsoleLog("❌ Ошибка: Локальный CodeExecutor не найден, а сервер отключен!", true);
            }
        }
    }

    public void OnExecutionFinished()
    {
        Invoke(nameof(CheckAfterExecution), 0.3f);
    }

    void CheckAfterExecution()
    {
        if (levelCompleted) return;

        if (progressMadeThisRun)
        {
            if (uiManager != null && uiManager.codeEditor != null)
            {
                uiManager.codeEditor.AddConsoleLog("👍 Отличная работа! Продолжай писать код дальше.", false);
            }
        }
        else
        {
            OnLevelFailed();
        }
    }

    void OnLevelFailed()
    {
        failedAttempts++;
        if (currentLevel == null) return;

        Debug.Log($"[LevelGameManager] ❌ ПРОВАЛ! Попытка {failedAttempts}");

        if (uiManager != null && uiManager.codeEditor != null)
        {
            uiManager.codeEditor.AddConsoleLog($"❌ Попытка {failedAttempts}. Попробуй ещё раз!");
        }

        int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;

        if (failedAttempts >= attemptsPerHint * 4)
        {
            currentHintIndex = 4;
            if (uiManager != null)
            {
                uiManager.ShowSolutionButton();
                if (uiManager.codeEditor != null) uiManager.codeEditor.AddConsoleLog("💡 Кнопка 'Решение' доступна!");
            }
        }
        else if (failedAttempts >= attemptsPerHint)
        {
            currentHintIndex = failedAttempts / attemptsPerHint;
            currentHintIndex = Mathf.Clamp(currentHintIndex, 1, 3);
            if (uiManager != null)
            {
                uiManager.EnableHintButton();
                if (uiManager.codeEditor != null) uiManager.codeEditor.AddConsoleLog("💡 Подсказка доступна! Нажми кнопку 'Подсказка'");
            }
        }
    }

    public void OnShowHint()
    {
        if (currentLevel == null || uiManager == null) return;

        string hint = GetCurrentHint();
        if (!string.IsNullOrEmpty(hint))
        {
            uiManager.ShowHint(hint);
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
        if (currentLevel == null || uiManager == null) return;
        hintsUsedCount++;

        if (uiManager.codeEditor != null && !string.IsNullOrEmpty(currentLevel.solutionCode))
        {
            uiManager.codeEditor.SetCode(currentLevel.solutionCode);
            uiManager.codeEditor.AddConsoleLog("💡 Загружено правильное решение. Нажми Run!");
        }

        if (uiManager.useSolutionButton != null)
            uiManager.useSolutionButton.gameObject.SetActive(false);
    }

    public void OnResetLevel()
    {
        if (CodeExecutor.Instance != null) CodeExecutor.Instance.StopExecution();
        if (player != null) player.ResetState();

        if (currentLevel != null)
        {
            if (uiManager != null && uiManager.codeEditor != null)
            {
                uiManager.codeEditor.SetCode(currentLevel.starterCode);
                uiManager.codeEditor.ClearConsole();
            }

            if (player != null) player.SetStartPosition(currentLevel.playerStartPosition);
            levelCompleted = false;
        }

        // Пробрасываем сброс в ElevatorLevelController для кат-сцены и лифтов
        ElevatorLevelController elc = FindFirstObjectByType<ElevatorLevelController>();
        if (elc != null)
        {
            elc.ResetLevel();
        }
    }

    void Update()
    {
        if (!levelCompleted && player != null && goalTransform != null)
        {
            if (Vector2.Distance(player.transform.position, goalTransform.position) < 0.5f)
            {
                OnLevelCompleted();
            }
        }
    }

    void OnLevelCompleted()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        if (CodeExecutor.Instance != null) CodeExecutor.Instance.StopExecution();

        int completionTime = Mathf.RoundToInt(Time.time - levelStartTime);
        int stars = CalculateStars(failedAttempts, hintsUsedCount, completionTime);
        int codeLines = uiManager != null && uiManager.codeEditor != null ? CountCodeLines(uiManager.codeEditor.GetCode()) : 0;
        int attemptsTotal = failedAttempts + 1;

        if (uiManager != null)
        {
            if (uiManager.victoryPanelUI != null) uiManager.victoryPanelUI.SetStats(completionTime, attemptsTotal, stars, 0);

            if (uiManager.codeEditor != null)
            {
                uiManager.codeEditor.AddConsoleLog("🎉 Уровень пройден!");
                uiManager.codeEditor.AddConsoleLog($"⭐ Звёзд: {stars}/3");
                uiManager.codeEditor.AddConsoleLog($"⏱️ Время: {completionTime}с");
                uiManager.codeEditor.AddConsoleLog($"📊 Попыток: {attemptsTotal}");
                
                if (failedAttempts == 0) uiManager.codeEditor.AddConsoleLog("🏆 Идеально! Решено с первой попытки!");
                else if (failedAttempts <= 2) uiManager.codeEditor.AddConsoleLog($"✨ Отлично!");
            }

            if (uiManager.victoryPanel != null) uiManager.victoryPanel.SetActive(true);
        }

        if (currentLevel != null)
        {
            StartCoroutine(ProgressAPIService.Instance.SaveLevelCompletion(
                currentLevel.levelId, stars, completionTime, failedAttempts, hintsUsedCount, codeLines,
                OnProgressSaved, OnProgressSaveError
            ));
        }
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
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//")) count++;
        }
        return count;
    }

    void OnProgressSaved(ProgressAPIService.LevelCompletionResponse response)
    {
        if (uiManager != null)
        {
            if (uiManager.victoryPanelUI != null)
            {
                int completionTime = Mathf.RoundToInt(Time.time - levelStartTime);
                int attemptsTotal = failedAttempts + 1;
                int stars = CalculateStars(failedAttempts, hintsUsedCount, completionTime);
                uiManager.victoryPanelUI.SetStats(completionTime, attemptsTotal, stars, response.data.xpGained);
            }

            if (uiManager.codeEditor != null)
            {
                uiManager.codeEditor.AddConsoleLog($"💰 +{response.data.xpGained} XP");
                uiManager.codeEditor.AddConsoleLog($"🎯 Уровень: {response.data.stats.level}");

                if (response.data.achievements != null)
                {
                    foreach (var ach in response.data.achievements)
                        if (ach.isNew) uiManager.codeEditor.AddConsoleLog($"🏆 {ach.name}");
                }
            }
        }
    }

    void OnProgressSaveError(string error)
    {
        if (uiManager != null && uiManager.codeEditor != null)
            uiManager.codeEditor.AddConsoleLog("⚠️ Прогресс не сохранён (offline)");
    }

    public void OnNextLevel()
    {
        Debug.Log("[LevelGameManager] Возврат в меню уровней (т.к. каждый уровень теперь на отдельной сцене)");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelectScene");
    }
}
