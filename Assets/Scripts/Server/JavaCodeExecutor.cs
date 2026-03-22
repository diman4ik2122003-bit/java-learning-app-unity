using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class JavaCodeExecutor : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:4000/api/v1/submissions/execute";
    
    private CodeEditorUIToolkit codeEditor;
    private PlayerController player;
    
    void Start()
    {
        codeEditor = FindFirstObjectByType<CodeEditorUIToolkit>();
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
        
        StartCoroutine(SendCodeToServer(code));
    }
    
    IEnumerator SendCodeToServer(string code)
    {
        codeEditor.AddConsoleLog("⏳ Отправка кода на сервер...");
        
        int levelId = 1;
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null && levelManager.allLevels != null)
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
    
    IEnumerator ExecuteCommandsSequence(GameCommand[] commands)
    {
        foreach (var cmd in commands)
        {
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
                    
                case "lowerElevator":
                    ElevatorLevelController elevatorController = FindFirstObjectByType<ElevatorLevelController>();
                    if (elevatorController != null)
                    {
                        yield return elevatorController.LowerElevator((int)cmd.value2, cmd.value);
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
        
        // ⭐ КРИТИЧНО: ВСЕГДА вызываем LevelManager
        CallExecutionFinished();
    }

    // ⭐ Единая точка вызова OnExecutionFinished
    void CallExecutionFinished()
    {
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] LevelManager не найден!");
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
