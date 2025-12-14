using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ConsoleController : MonoBehaviour
{
    public static ConsoleController Instance;

    public TMP_Text consoleText;
    public ScrollRect scrollRect;
    public int maxLines = 50;

    private List<string> logLines = new List<string>();
    private bool isUpdating = false;  // защита от рекурсии

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // НЕ перехватываем все Unity логи, только наши
        Clear();
    }

    public void AddLog(string message, LogType type = LogType.Log)
    {
        if (isUpdating) return;  // защита от рекурсии

        string coloredMessage = "";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                coloredMessage = $"<color=#FF5555>❌ {message}</color>";
                break;
            case LogType.Warning:
                coloredMessage = $"<color=#FFAA00>⚠️ {message}</color>";
                break;
            default:
                coloredMessage = $"<color=#CCCCCC>{message}</color>";
                break;
        }

        logLines.Add(coloredMessage);

        if (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        UpdateDisplay();
        StartCoroutine(ScrollToBottomNextFrame());
    }

    public void Clear()
    {
        if (isUpdating) return;

        logLines.Clear();
        logLines.Add("<color=#888888>Console ready.</color>");
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (consoleText == null || isUpdating) return;

        isUpdating = true;

        consoleText.text = string.Join("\n", logLines);

        isUpdating = false;
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return null;

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
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
