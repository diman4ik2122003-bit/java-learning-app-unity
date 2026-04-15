using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Text.RegularExpressions;

public class JavaCodeExecutor : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:4000/api/v1/submissions/execute";
    
    private CodeEditor codeEditor;
    private PlayerController player;
    
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
            codeEditor.AddConsoleLog("❌ Ко�� пустой!", true);
            CallExecutionFinished();
            return;
        }

        // --- Для AlchemyLevelManager: определяем тип данных ---
        AlchemyLevelManager alm = FindFirstObjectByType<AlchemyLevelManager>();
        if (alm != null)
        {
            alm.DetectDataType(code);
            Debug.Log("[JavaCodeExecutor] AlchemyLevelManager detected");
        }
        // -------------------------------------------------------

        // --- Геймификация: проверка открытых типов данных (для лифта) ---
        ElevatorLevelController elc = FindFirstObjectByType<ElevatorLevelController>();
        if (elc != null)
        {
            string cleanCode = Regex.Replace(code, @"//.*", "");
            cleanCode = Regex.Replace(cleanCode, @"/\*.*?\*/", "", RegexOptions.Singleline);

            string[] restrictedTypes = { "short", "int", "long", "float", "double" };
            foreach (var type in restrictedTypes)
            {
                if (Regex.IsMatch(cleanCode, $@"\b{type}\b"))
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
        // ---------------------------------------------------------------
        
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
                        if (!string.IsNullOrEmpty(result.output))
                        {
                            var mc1 = Regex.Matches(result.output, @"\""value\""\s*:\s*(-?\d+)");
                            var mc2 = Regex.Matches(result.output, @"\""value2\""\s*:\s*(-?\d+)");
                            for (int i = 0; i < result.commands.Length; i++)
                            {
                                if (i < mc1.Count && long.TryParse(mc1[i].Groups[1].Value, out long trueVal))
                                    result.commands[i].value = trueVal;
                                else if (i < mc1.Count && ulong.TryParse(mc1[i].Groups[1].Value, out ulong _))
                                    result.commands[i].value = long.MaxValue;

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
                        CallExecutionFinished();
                    }
                    else if (result.status == "runtime_error")
                    {
                        codeEditor.AddConsoleLog(result.error, true);
                        CallExecutionFinished();
                    }
                    else
                    {
                        codeEditor.AddConsoleLog("❌ " + (result.error ?? "Неизвестная ошибка"), true);
                        CallExecutionFinished();
                    }
                }
                else
                {
                    codeEditor.AddConsoleLog("❌ Ошибка обработки ответа сервера", true);
                    CallExecutionFinished();
                }
            }
            else
            {
                codeEditor.AddConsoleLog("❌ Ошибка соединения с сервером", true);
                codeEditor.AddConsoleLog($"Детали: {www.error}", true);
                Debug.LogError("[JavaCodeExecutor] Network error: " + www.error);
                CallExecutionFinished();
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

    IEnumerator ExecuteCommandsSequence(GameCommand[] commands)
    {
        executionAborted = false;
        
        // ⭐ Считаем количество addDrop команд
        int dropsCount = 0;
        foreach (var cmd in commands)
        {
            if (cmd.action == "addDrop")
                dropsCount += (int)cmd.value;
        }
        
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

                case "addDrop":
                    AlchemyLevelManager almExec = FindFirstObjectByType<AlchemyLevelManager>();
                    if (almExec != null)
                    {
                        almExec.OnAddDrop((int)cmd.value);
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
        
        // ⭐ ВЫЗЫВАЕМ AlchemyLevelManager с количеством капель
        AlchemyLevelManager almMgr = FindFirstObjectByType<AlchemyLevelManager>();
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (almMgr != null)
        {
            Debug.Log($"[JavaCodeExecutor] 🎯 Вызываем AlchemyLevelManager.OnExecutionFinished() с {dropsCount} каплями");
            almMgr.OnExecutionFinished(dropsCount);
        }
        else if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] 🎯 Вызываем LevelGameManager.OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] LevelGameManager not found!");
        }
    }

    void CallExecutionFinished()
    {
        AlchemyLevelManager almMgr = FindFirstObjectByType<AlchemyLevelManager>();
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (almMgr != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем AlchemyLevelManager.OnExecutionFinished() с 0 каплями");
            almMgr.OnExecutionFinished(0);
        }
        else if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем LevelGameManager.OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] No level manager found!");
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