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
    private bool isUpdating = false;  // защита от рекурсии

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
            Debug.LogError("[ConsoleController] consoleInputField is NULL! Drag the TMP_Text from UI into the slot.");
            return;
        }

        // Очищаем сообщение от эмодзи ДЛЯ консоли (фонт их не видит)
        message = StripEmojis(message);

        string coloredMessage = "";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                coloredMessage = $"<color=#9C7552>[X] {message}</color>";
                break;
            case LogType.Warning:
                coloredMessage = $"<color=#9C7552>[!] {message}</color>";
                break;
            default:
                coloredMessage = $"<color=#9C7552>{message}</color>";
                break;
        }

        logLines.Add(coloredMessage);

        if (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        UpdateDisplay();
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private string StripEmojis(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Заменяем популярные эмодзи на текст
        text = text.Replace("✅", "[OK]").Replace("❌", "[X]").Replace("⚠️", "[!]").Replace("▶️", "[>>]").Replace("⏱️", "[T]");
        // Удаляем все остальные не-ASCII и не-Кириллические символы
        return Regex.Replace(text, @"[^\u0000-\u007F\u0400-\u052F\u00A0-\u024F]", "?");
    }

    public void Clear()
    {
        if (isUpdating) return;

        logLines.Clear();
        logLines.Add("<color=#BBBBBB>Console ready.</color>");
        UpdateDisplay();
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
