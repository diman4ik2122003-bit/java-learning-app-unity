using UnityEngine;
using System.Collections.Generic;

public class HintSystem : MonoBehaviour
{
    [Header("Hint Settings")]
    [Tooltip("Количество неудачных попыток перед первой подсказкой")]
    public int attemptsBeforeFirstHint = 3;
    
    [Tooltip("Интервал между подсказками (в попытках)")]
    public int hintInterval = 2;
    
    [Header("Hint Levels")]
    [TextArea(2, 4)]
    public List<string> hints = new List<string>();
    
    [TextArea(5, 10)]
    public string correctCode = "";
    
    [Header("References")]
    private CodeEditor codeEditor;
    private LevelManager levelManager;
    
    private int failedAttempts = 0;
    private int currentHintIndex = 0;
    private bool levelCompleted = false;
    
    void Start()
    {
        codeEditor = FindFirstObjectByType<CodeEditor>();
        levelManager = FindFirstObjectByType<LevelManager>();
        
        if (hints.Count == 0)
        {
            // Подсказки по умолчанию
            hints.Add("💡 Совет: Проверь значения переменных distanceRight и distanceUp");
            hints.Add("💡 Совет: Игрок должен двигаться вправо на 10 клеток, затем вверх на 5");
            hints.Add("💡 Подсказка: Попробуй использовать Player.moveRight(10); и Player.moveUp(5);");
        }
    }
    
    // Вызывается когда игрок НЕ прошёл уровень
    public void OnLevelFailed()
    {
        if (levelCompleted) return;
        
        failedAttempts++;
        
        Debug.Log($"Failed attempts: {failedAttempts}");
        
        // Проверяем нужна ли подсказка
        if (ShouldShowHint())
        {
            ShowNextHint();
        }
        else
        {
            // Показываем количество попыток до подсказки
            int attemptsUntilHint = GetAttemptsUntilNextHint();
            if (attemptsUntilHint > 0 && codeEditor != null)
            {
                codeEditor.AddConsoleLog($"❌ Попытка {failedAttempts}. Ещё {attemptsUntilHint} попыток до подсказки");
            }
        }
    }
    
    // Вызывается когда игрок прошёл уровень
    public void OnLevelCompleted()
    {
        levelCompleted = true;
        failedAttempts = 0;
        currentHintIndex = 0;
        
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog("🎉 Уровень пройден!");
        }
    }
    
    bool ShouldShowHint()
    {
        // Первая подсказка после attemptsBeforeFirstHint попыток
        if (failedAttempts == attemptsBeforeFirstHint)
        {
            return true;
        }
        
        // Последующие подсказки через hintInterval попыток
        if (failedAttempts > attemptsBeforeFirstHint)
        {
            int attemptsSinceFirstHint = failedAttempts - attemptsBeforeFirstHint;
            return attemptsSinceFirstHint % hintInterval == 0;
        }
        
        return false;
    }
    
    int GetAttemptsUntilNextHint()
    {
        if (failedAttempts < attemptsBeforeFirstHint)
        {
            return attemptsBeforeFirstHint - failedAttempts;
        }
        
        int attemptsSinceFirstHint = failedAttempts - attemptsBeforeFirstHint;
        int attemptsInCurrentCycle = attemptsSinceFirstHint % hintInterval;
        return hintInterval - attemptsInCurrentCycle;
    }
    
    void ShowNextHint()
    {
        if (codeEditor == null) return;
        
        // Если подсказки закончились - показываем правильный код
        if (currentHintIndex >= hints.Count)
        {
            ShowCorrectCode();
            return;
        }
        
        // Показываем текущую подсказку
        string hint = hints[currentHintIndex];
        codeEditor.AddConsoleLog($"\n{hint}\n");
        
        currentHintIndex++;
    }
    
    void ShowCorrectCode()
    {
        if (codeEditor == null) return;
        
        codeEditor.AddConsoleLog("\n📝 Правильное решение:");
        codeEditor.AddConsoleLog("──────────────────────");
        
        if (!string.IsNullOrEmpty(correctCode))
        {
            codeEditor.SetCode(correctCode);
            codeEditor.AddConsoleLog("✓ Код вставлен в редактор. Нажми Run Code!");
        }
        else
        {
            codeEditor.AddConsoleLog("⚠️ Правильный код не настроен для этого уровня");
        }
    }
    
    // Сброс при Reset уровня
    public void OnLevelReset()
    {
        // НЕ сбрасываем failedAttempts и currentHintIndex
        // Они сбрасываются только при успешном прохождении
        
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog($"⟲ Попытка {failedAttempts + 1}");
        }
    }
    
    // Для отладки
    public void ResetHints()
    {
        failedAttempts = 0;
        currentHintIndex = 0;
        levelCompleted = false;
        
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog("🔄 Подсказки сброшены");
        }
    }
}
