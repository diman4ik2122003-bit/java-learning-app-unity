using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class ConsoleController : MonoBehaviour
{
    public static ConsoleController Instance;

    public TMP_InputField consoleInputField;
    public int maxLines = 50;

    private List<string> logLines = new List<string>();
    private bool isUpdating = false; 
    private bool isDirty = false; // Флаг для отложенного обновления

    public bool interceptUnityLogs = true;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void OnEnable()
    {
        if (interceptUnityLogs)
            Application.logMessageReceived += HandleUnityLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        // Игнорируем мусор от шрифтов и внутренние логи разработки
        if (logString.Contains("Unicode value") || 
            logString.Contains("font asset") ||
            logString.Contains("Start position set") ||
            logString.Contains("[LevelGameManager]") ||
            logString.Contains("Сундук открыт") ||
            logString.Contains("RUN Button Clicked") ||
            logString.Contains("[CodeEditorButtonBridge]") ||
            logString.Contains("[JavaCodeExecutor]") ||
            logString.Contains("Server response:") ||
            logString.Contains("Level ID:") ||
            logString.Contains("Вызываем LevelGameManager") ||
            logString.Contains("[ElevatorController]") ||
            logString.Contains("[Pulley]") ||
            logString.Contains("[Gamification]") ||
            logString.Contains("Найден новый тип данных") ||
            logString.Contains("[ProgressAPI]") ||
            logString.Contains("ResetState called") ||
            logString.Contains("[AlchemyLevelManager]") ||
            logString.Contains("[VictoryPanelUI]") ||
            logString.Contains("JobTempAlloc") ||
            logString.Contains("Level completed") ||
            logString.Contains("Stars:") ||
            logString.Contains("Attempts:") ||
            logString.Contains("Time:") ||
            logString.Contains("Идеально! Решено с первой попытки"))
            return;
            
        AddLog(logString, type);
    }

    void Start()
    {
        // НЕ перехватываем все Unity логи, только наши
        Clear();
    }

    public void AddLog(string message, LogType type = LogType.Log)
    {
        if (isUpdating) return;
        
        if (consoleInputField == null) 
        {
            // Убираем Debug.LogError, чтобы избежать бесконечной рекурсии (т.к. мы перехватываем логи)
            return;
        }

        // Очищаем сообщение от эмодзи ДЛЯ консоли (фонт их не видит)
        message = StripEmojis(message);

        string coloredMessage = "";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                coloredMessage = $"<color=#9C7552>{message}</color>";
                break;
            case LogType.Warning:
                coloredMessage = $"<color=#9C7552>{message}</color>";
                break;
            default:
                coloredMessage = $"<color=#9C7552>{message}</color>";
                break;
        }

        logLines.Add(coloredMessage);

        if (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        isDirty = true; // Просто помечаем, что нужно обновиться
    }

    private string StripEmojis(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Заменяем популярные эмодзи на текст
        text = text.Replace("✅", "[OK]").Replace("❌", "[X]").Replace("⚠️", "[!]").Replace("▶️", "[>>]").Replace("⏱️", "[T]");
        // Удаляем все остальные не-ASCII и не-Кириллические символы
        // Удаляем все остальные не-ASCII и не-Кириллические символы, чтобы не было ??
        return Regex.Replace(text, @"[^\u0000-\u007F\u0400-\u052F\u00A0-\u024F]", "");
    }

    public void Clear()
    {
        logLines.Clear();
        logLines.Add("<color=#BBBBBB>Console ready.</color>");
        isDirty = true;
    }

    void LateUpdate()
    {
        if (isDirty)
        {
            UpdateDisplay();
            isDirty = false;
        }
    }

    void UpdateDisplay()
    {
        if (consoleInputField == null || isUpdating) return;

        isUpdating = true;
        consoleInputField.text = string.Join("\n", logLines);
        isUpdating = false;

        StartCoroutine(ScrollToBottomNextFrame());
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (consoleInputField != null)
        {
            // Scrolling to the end of the text
            consoleInputField.caretPosition = consoleInputField.text.Length;
        }
    }

    // Публичные методы для вызова из кода (НЕ через Application.logMessageReceived)
    public static void Log(string message)
    {
        if (Instance != null)
            Instance.AddLog(message, LogType.Log);
    }

    public static void LogError(string message)
    {
        if (Instance != null)
            Instance.AddLog(message, LogType.Error);
    }

    public static void LogWarning(string message)
    {
        if (Instance != null)
            Instance.AddLog(message, LogType.Warning);
    }
}
