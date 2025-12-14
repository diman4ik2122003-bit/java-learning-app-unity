using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class JavaCodeExecutor : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverUrl = "http://localhost:3000/api/submissions/execute";
    
    private CodeEditorUIToolkit codeEditor;
    private PlayerController player;
    
    void Start()
    {
        codeEditor = FindObjectOfType<CodeEditorUIToolkit>();
        player = FindObjectOfType<PlayerController>();
    }
    
    public void ExecuteCode()
    {
        if (codeEditor == null)
        {
            Debug.LogError("CodeEditor not found!");
            return;
        }
        
        string code = codeEditor.GetCode();
        StartCoroutine(SendCodeToServer(code));
    }
    
    IEnumerator SendCodeToServer(string code)
    {
        // Показываем индикатор загрузки
        codeEditor.AddConsoleLog("⏳ Отправка кода на сервер...");
        
        // Создаём JSON
        var request = new ExecutionRequest { code = code, levelId = 1 };
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
                
                try
                {
                    ExecutionResult result = JsonUtility.FromJson<ExecutionResult>(responseJson);
                    
                    if (result.success && result.status == "success")
                    {
                        codeEditor.AddConsoleLog("✅ Код выполнен успешно!");
                        ExecuteCommands(result.commands);
                    }
                    else
                    {
                        // Ошибка компиляции или выполнения
                        codeEditor.AddConsoleLog(result.error, true);
                        
                        // Показываем детали в консоли Unity
                        if (!string.IsNullOrEmpty(result.details))
                        {
                            Debug.Log("Детали ошибки:\n" + result.details);
                        }
                    }
                }
                catch (Exception e)
                {
                    codeEditor.AddConsoleLog("❌ Ошибка обработки ответа: " + e.Message, true);
                    Debug.LogError("Response: " + responseJson);
                }
            }
            else
            {
                codeEditor.AddConsoleLog("❌ Ошибка соединения с сервером", true);
                Debug.LogError("Network error: " + www.error);
            }
        }
    }
    
    void ExecuteCommands(GameCommand[] commands)
    {
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }
        
        StartCoroutine(ExecuteCommandsSequence(commands));
    }
    
    IEnumerator ExecuteCommandsSequence(GameCommand[] commands)
    {
        foreach (var cmd in commands)
        {
            codeEditor.AddConsoleLog($"▶️ {cmd.action}({cmd.value})");
            
            switch (cmd.action)
            {
                case "moveRight":
                    yield return player.MoveRightCoroutine(cmd.value);
                    break;
                    
                case "moveLeft":
                    yield return player.MoveLeftCoroutine(cmd.value);
                    break;
                    
                case "jump":
                    yield return player.JumpCoroutine(cmd.value);
                    break;
                    
                case "wait":
                    yield return new WaitForSeconds(cmd.value * 0.1f);
                    break;
            }
            
            yield return new WaitForSeconds(0.2f); // пауза между командами
        }
        
        codeEditor.AddConsoleLog("🎉 Выполнение завершено!");
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
    public int value;
}
