using UnityEngine;
using System.Collections;
using System;

public class LevelGameManager : MonoBehaviour
{
    [Header("Localization")]
    public LocalizedTextDatabase localizationDB;

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
    [Tooltip("Нужно ли проверять достижение цели по дистанции до goalTransform?")]
    public bool checkGoalDistance = true;

    [Header("Runtime Status")]
    public bool progressMadeThisRun = false;
    private bool isExecutionActive = false; // Блокировка повторных отчетов за один запуск

    // Системные счетчики
    public int failedAttempts = 0; // Сделаем публичным для удобства
    public int currentStepIndex = 0; // Текущий этап (например, номер лифта)
    private int hintsUsedCount = 0;
    private int currentHintIndex = 0;
    private bool levelCompleted = false;
    private float levelStartTime = 0f;
    private Collider2D goalCollider; // Кэшируем коллайдер цели

    private float lastFinishedTime = 0f;

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
            LoadLevelDirectly(LevelSelectionManager.SelectedLevel);
            LevelSelectionManager.SelectedLevel = null;
        }
        else if (currentLevel != null)
        {
            LoadLevelDirectly(currentLevel);
        }
        else
        {
            Debug.LogError("[LevelGameManager] Нет уровней для загрузки!");
        }

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged(string langCode)
    {
        if (currentLevel != null) ApplyLocalization(currentLevel);
    }
    void ApplyLocalization(LevelData level)
    {
        string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLang : "ru";

        string name = (lang == "en" && !string.IsNullOrEmpty(level.levelName_en))
            ? level.levelName_en : level.levelName;
        string desc = (lang == "en" && !string.IsNullOrEmpty(level.description_en))
            ? level.description_en : level.description;
        string code = (lang == "en" && !string.IsNullOrEmpty(level.starterCode_en))
            ? level.starterCode_en : level.starterCode;

        if (uiManager != null)
        {
            string group = (lang == "en" && !string.IsNullOrEmpty(level.groupName_en))
                ? level.groupName_en : level.groupName;

            uiManager.SetTaskInfo($"{group} - {name}", desc);
            if (uiManager.codeEditor != null)
                uiManager.codeEditor.SetCode(code);
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
            ApplyLocalization(level);
            
            if (uiManager.codeEditor != null)
            {
                ApplyLocalization(level);
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
            // Ищем коллайдер на самом объекте или в его детях
            goalCollider = goalTransform.GetComponentInChildren<Collider2D>();
        }

        Debug.Log($"[LevelGameManager] ✅ Загружен уровень: {level.levelId}");
    }

    public void OnRunCode()
    {
        isExecutionActive = true;

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
            BridgeLevelController blc = FindFirstObjectByType<BridgeLevelController>();
            if (blc == null)
            {
                player.ResetState();
            }
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
        // Если мы уже обработали результат этого запуска или запуск не активен — игнорируем
        if (!isExecutionActive) return;
        
        isExecutionActive = false; // СРАЗУ закрываем окно для повторных вызовов
        Invoke(nameof(CheckAfterExecution), 0.3f);
    }

    void CheckAfterExecution()
    {
        if (levelCompleted) return;
        // Флаг теперь сбрасывается в OnExecutionFinished

        if (progressMadeThisRun)
        {
            failedAttempts = 0; // Сбрасываем ошибки при любом прогрессе
            
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

    public void OnLevelFailed()
    {
        failedAttempts++;
        if (currentLevel == null) return;

        Debug.Log($"[LevelGameManager] ❌ ПРОВАЛ! Попытка {failedAttempts}");

        if (uiManager != null && uiManager.codeEditor != null)
        {
            uiManager.codeEditor.AddConsoleLog($"Попытка {failedAttempts}. Попробуй ещё раз!", true);
        }

        int attemptsPerHint = currentLevel.attemptsBeforeFirstHint;

        if (failedAttempts >= attemptsPerHint * 4)
        {
            currentHintIndex = 4;
            if (uiManager != null)
            {
                uiManager.ShowSolutionButton();
            }
        }
        else if (failedAttempts >= attemptsPerHint)
        {
            currentHintIndex = failedAttempts / attemptsPerHint;
            currentHintIndex = Mathf.Clamp(currentHintIndex, 1, 3);
            if (uiManager != null)
            {
                uiManager.EnableHintButton();
                
                // ⭐ АВТО-ОФФЕР: Открываем панель, но без текста (с кнопкой "Получить")
                uiManager.OpenHintOffer();
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
        string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLang : "ru";

        // ⭐ НОВАЯ СИСТЕМА: Сначала пробуем шаги
        if (currentLevel.steps != null && currentLevel.steps.Count > 0)
        {
            int stepIdx = Mathf.Clamp(currentStepIndex, 0, currentLevel.steps.Count - 1);
            LevelStep step = currentLevel.steps[stepIdx];
            
            if (step.hints != null && step.hints.Count > 0)
            {
                // Выбираем под-подсказку внутри шага на основе количества ошибок на ЭТОМ шаге
                int hintInStepIdx = (failedAttempts / currentLevel.attemptsBeforeFirstHint) - 1;
                hintInStepIdx = Mathf.Clamp(hintInStepIdx, 0, step.hints.Count - 1);
                return step.hints[hintInStepIdx];
            }
        }

        // ⭐ СТАРАЯ СИСТЕМА: Если шаги не заполнены (hint1, hint2, hint3)
        string h1 = (lang == "en" && !string.IsNullOrEmpty(currentLevel.hint1_en)) ? currentLevel.hint1_en : currentLevel.hint1;
        string h2 = (lang == "en" && !string.IsNullOrEmpty(currentLevel.hint2_en)) ? currentLevel.hint2_en : currentLevel.hint2;
        string h3 = (lang == "en" && !string.IsNullOrEmpty(currentLevel.hint3_en)) ? currentLevel.hint3_en : currentLevel.hint3;

        switch (currentHintIndex)
        {
            case 1: return h1;
            case 2: return h2;
            case 3: return h3;
            default: return currentLevel.hint;
        }
    }

    // Метод для контроллеров уровней, чтобы сообщать о прогрессе
    public void ReportProgress()
    {
        progressMadeThisRun = true;
        currentStepIndex++;
        failedAttempts = 0; // Обнуляем ошибки при переходе на новый этап
        Debug.Log($"[LevelGameManager] Прогресс! Текущий шаг: {currentStepIndex}");
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
        if (JavaCodeExecutor.Instance != null) JavaCodeExecutor.Instance.StopExecution();
        
        if (player != null) player.ResetState();

        if (currentLevel != null)
        {
            if (uiManager != null && uiManager.codeEditor != null)
            {
                string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLang : "ru";
                string code = (lang == "en" && !string.IsNullOrEmpty(currentLevel.starterCode_en))
                    ? currentLevel.starterCode_en : currentLevel.starterCode;
                uiManager.codeEditor.SetCode(code);

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

        // Пробрасываем сброс в BridgeLevelController
        BridgeLevelController blc = FindFirstObjectByType<BridgeLevelController>();
        if (blc != null)
        {
            blc.ResetLevel();
        }
    }

    void Update()
    {
        if (!levelCompleted && player != null && goalTransform != null)
        {
            Vector2 playerPos = player.transform.position;
            Vector2 goalPos = goalTransform.position;
            float dist = Vector2.Distance(playerPos, goalPos);

            bool reachedByDistance = checkGoalDistance && dist < 1.4f;
            bool reachedByCollider = (goalCollider != null && goalCollider.OverlapPoint(playerPos));

            if (reachedByDistance || reachedByCollider)
            {
                OnLevelCompleted();
            }
        }
    }

    // Позволяет засчитывать победу через триггеры (если на Цели стоит коллайдер)
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!levelCompleted && other.CompareTag("Player"))
        {
            OnLevelCompleted();
        }
    }

    public void OnLevelCompleted()
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
                string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLang : "ru";
                
                if (localizationDB != null)
                {
                    uiManager.codeEditor.AddConsoleLog(localizationDB.Get("level_completed", lang));
                    uiManager.codeEditor.AddConsoleLog(string.Format(localizationDB.Get("level_stars", lang), stars));
                    uiManager.codeEditor.AddConsoleLog(string.Format(localizationDB.Get("level_time", lang), completionTime));
                    uiManager.codeEditor.AddConsoleLog(string.Format(localizationDB.Get("level_attempts", lang), attemptsTotal));
                }
                
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
        if (currentLevel == null) return 1;

        int stars = 3;
        
        // Проверка по попыткам
        if (attempts > currentLevel.attemptsFor2Stars) stars = 1;
        else if (attempts > currentLevel.attemptsFor3Stars) stars = 2;
        
        // Штраф за подсказки
        if (hints > 0 && stars > 1) stars--;
        
        // Штраф за время
        if (time > currentLevel.timeFor3Stars && stars > 1) stars--;
        
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
