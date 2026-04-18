using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class JavaCodeExecutor : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:4000/api/v1/submissions/execute";
    
    public static JavaCodeExecutor Instance;
    
    private CodeEditor codeEditor;
    private PlayerController player;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        codeEditor = FindFirstObjectByType<CodeEditor>();
        player = FindFirstObjectByType<PlayerController>();
    }
    
    public void ExecuteCode()
    {
        if (codeEditor == null)
        {
            Debug.LogError("[JavaCodeExecutor] CodeEditor not found!");
            return;
        }
        
        if (player == null)
        {
            Debug.LogError("[JavaCodeExecutor] Player not found!");
            return;
        }
        
        string code = codeEditor.GetCode();
        
        if (string.IsNullOrWhiteSpace(code))
        {
            codeEditor.AddConsoleLog("❌ Код пустой!", true);
            CallExecutionFinished(); // ← Даже при пустом коде вызываем!
            return;
        }

        // --- Геймификация: проверка открытых типов данных ---
        ElevatorLevelController elc = FindFirstObjectByType<ElevatorLevelController>();
        if (elc != null)
        {
            // Очищаем код от комментариев, чтобы подсказки // не блокировали запуск
            string cleanCode = System.Text.RegularExpressions.Regex.Replace(code, @"//.*", "");
            cleanCode = System.Text.RegularExpressions.Regex.Replace(cleanCode, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);

            string[] restrictedTypes = { "short", "int", "long", "float", "double" };
            foreach (var type in restrictedTypes)
            {
                // Ищем точное совпадение слова (например, "short ")
                if (System.Text.RegularExpressions.Regex.IsMatch(cleanCode, $@"\b{type}\b"))
                {
                    if (!elc.unlockedTypes.Contains(type))
                    {
                        codeEditor.AddConsoleLog($"\n🚫 ОШИБКА: Вы пытаетесь использовать тип '{type}', но ваш персонаж еще не нашел его в сундуке!\nДоберитесь до сундука используя уже открытые типы.", true);
                        CallExecutionFinished();
                        return;
                    }
                }
            }
        }
        // ----------------------------------------------------
        
        StartCoroutine(SendCodeToServer(code));
    }
    
    IEnumerator SendCodeToServer(string code)
    {
        codeEditor.AddConsoleLog("⏳ Отправка кода на сервер...");
        
        int levelId = 1;
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        if (levelManager != null)
        {
            // Simple logic: if we have a current level index, use it. 
            // In a real scenario we'd use currentLevel.levelId.
        }

        var request = new ExecutionRequest { code = code, levelId = levelId };
        string json = JsonUtility.ToJson(request);
        
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseJson = www.downloadHandler.text;
                Debug.Log("[JavaCodeExecutor] Server response: " + responseJson);
                
                ExecutionResult result = ParseResponse(responseJson);
                
                if (result != null)
                {
                    if (result.success && result.status == "success")
                    {
                        // ВОССТАНОВЛЕНИЕ EXACT LONG:
                        // Node.js округляет 64-битные числа, так что JsonUtility ставит 0 для огромных (т.к. они > long.MaxValue).
                        // Но в result.output числа лежат как точный текст от Java!
                        if (!string.IsNullOrEmpty(result.output))
                        {
                            var mc1 = System.Text.RegularExpressions.Regex.Matches(result.output, @"\""value\""\s*:\s*(-?\d+)");
                            var mc2 = System.Text.RegularExpressions.Regex.Matches(result.output, @"\""value2\""\s*:\s*(-?\d+)");
                            for (int i = 0; i < result.commands.Length; i++)
                            {
                                if (i < mc1.Count && long.TryParse(mc1[i].Groups[1].Value, out long trueVal))
                                    result.commands[i].value = trueVal;
                                else if (i < mc1.Count && ulong.TryParse(mc1[i].Groups[1].Value, out ulong _))
                                    result.commands[i].value = long.MaxValue; // Хак на случай если число вышло за границу

                                if (i < mc2.Count && long.TryParse(mc2[i].Groups[1].Value, out long trueVal2))
                                    result.commands[i].value2 = trueVal2;
                            }
                        }

                        codeEditor.AddConsoleLog("✅ Код скомпилирован!");
                        codeEditor.AddConsoleLog($"📝 Команд: {result.commands.Length}");
                        
                        yield return StartCoroutine(ExecuteCommandsSequence(result.commands));
                    }
                    else if (result.status == "compilation_error")
                    {
                        codeEditor.AddConsoleLog(result.error, true);
                        CallExecutionFinished(); // ← Ошибка компиляции = провал
                    }
                    else if (result.status == "runtime_error")
                    {
                        codeEditor.AddConsoleLog(result.error, true);
                        CallExecutionFinished(); // ← Ошибка выполнения = провал
                    }
                    else
                    {
                        codeEditor.AddConsoleLog("❌ " + (result.error ?? "Неизвестная ошибка"), true);
                        CallExecutionFinished(); // ← Любая ошибка = провал
                    }
                }
                else
                {
                    codeEditor.AddConsoleLog("❌ Ошибка обработки ответа сервера", true);
                    CallExecutionFinished(); // ← Ошибка парсинга = провал
                }
            }
            else
            {
                codeEditor.AddConsoleLog("❌ Ошибка соединения с сервером", true);
                codeEditor.AddConsoleLog($"Детали: {www.error}", true);
                Debug.LogError("[JavaCodeExecutor] Network error: " + www.error);
                CallExecutionFinished(); // ← Сетевая ошибка = провал
            }
        }
    }
    
    ExecutionResult ParseResponse(string json)
    {
        try
        {
            return JsonUtility.FromJson<ExecutionResult>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[JavaCodeExecutor] Parse error: " + e.Message);
            Debug.LogError("[JavaCodeExecutor] Response was: " + json);
            return null;
        }
    }
    
    public bool executionAborted = false;
    
    public void StopExecution()
    {
        executionAborted = true;
        StopAllCoroutines();
    }

    IEnumerator ExecuteCommandsSequence(GameCommand[] commands)
    {
        executionAborted = false;
        foreach (var cmd in commands)
        {
            if (executionAborted)
            {
                codeEditor.AddConsoleLog("🛑 Выполнение прервано из-за ошибки.", true);
                break;
            }

            codeEditor.AddConsoleLog($"▶️ {cmd.action}({cmd.value})");
            
            switch (cmd.action)
            {
                case "moveRight":
                    yield return player.MoveRightCoroutine((int)cmd.value);
                    break;
                    
                case "moveLeft":
                    yield return player.MoveLeftCoroutine((int)cmd.value);
                    break;
                    
                case "moveUp":
                    yield return player.MoveUpCoroutine((int)cmd.value);
                    break;
                    
                case "moveDown":
                    yield return player.MoveDownCoroutine((int)cmd.value);
                    break;
                    
                case "raiseElevator":
                    ElevatorLevelController elevatorController = FindFirstObjectByType<ElevatorLevelController>();
                    if (elevatorController != null)
                    {
                        yield return elevatorController.RaiseElevator((int)cmd.value2, cmd.value);
                    }
                    break;
                    
                case "addPlank":
                    BridgeLevelController bridgeController = FindFirstObjectByType<BridgeLevelController>();
                    if (bridgeController != null)
                    {
                        yield return bridgeController.AddPlank((int)cmd.value);
                    }
                    break;
                    
                case "wait":
                    yield return new WaitForSeconds(cmd.value * 0.1f);
                    break;
                    
                default:
                    codeEditor.AddConsoleLog($"⚠️ Неизвестная команда: {cmd.action}", true);
                    break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        codeEditor.AddConsoleLog("✅ Выполнение завершено!");
        
        // Для уровня с мостом запускаем попытку пройти по мосту в самом конце
        BridgeLevelController levelBridgeController = FindFirstObjectByType<BridgeLevelController>();
        if (levelBridgeController != null)
        {
            yield return levelBridgeController.WalkBridgeAndCheck();
        }
        
        // ⭐ КРИТИЧНО: ВСЕГДА вызываем LevelGameManager
        // чтобы проверить, провалился уровень или нет
        // Задержка небольшая, чтобы успеть доиграть анимации
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] 🎯 Вызываем OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] LevelGameManager не найден!");
        }
    }

    // ⭐ Единая точка вызова OnExecutionFinished
    void CallExecutionFinished()
    {
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] LevelGameManager не найден!");
        }
    }
}

[Serializable]
public class ExecutionRequest
{
    public string code;
    public int levelId;
}

[Serializable]
public class ExecutionResult
{
    public bool success;
    public string status;
    public string error;
    public string details;
    public string output;
    public GameCommand[] commands;
}

[Serializable]
public class GameCommand
{
    public string action;
    public long value;
    public long value2;
}
