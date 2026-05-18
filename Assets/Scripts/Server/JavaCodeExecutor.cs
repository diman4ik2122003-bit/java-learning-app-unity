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
    
    public static JavaCodeExecutor Instance;
    
    private CodeEditor codeEditor;
    private PlayerController player;
    private bool isExecuting = false;
    
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
        if (isExecuting) return;
        
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
        
        isExecuting = true;
        StartCoroutine(SendCodeToServer(code));
    }
    
    IEnumerator SendCodeToServer(string code)
    {
        // codeEditor.AddConsoleLog("Отправка кода на сервер..."); // Убираем этот лог
        
        int levelId = 1;
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (levelManager != null && levelManager.currentLevel != null)
        {
            string sid = levelManager.currentLevel.levelId;
            // Убираем тире, чтобы получилось 22 из 2-2
            string numericId = sid.Replace("-", "");
            int.TryParse(numericId, out levelId);
        }
        
        Debug.Log("[JavaCodeExecutor] Отправка кода для Level ID: " + levelId);

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

                        codeEditor.AddConsoleLog("[OK] Код скомпилирован!");
                        // codeEditor.AddConsoleLog($"Команд к выполнению: {result.commands.Length}");
                        
                        yield return StartCoroutine(ExecuteCommandsSequence(result.commands, result.output));
                    }
                    else if (result.status == "compilation_error")
                    {
                        string userCode = codeEditor.GetCode();
                        string friendlyError = AnalyzeJavaError(result.error, result.details, userCode);
                        codeEditor.AddConsoleLog(friendlyError, true);
                        CallExecutionFinishedCompileError();
                    }
                    else if (result.status == "runtime_error")
                    {
                        codeEditor.AddConsoleLog(result.error, true);
                        CallExecutionFinished();
                    }
                    else
                    {
                        codeEditor.AddConsoleLog("[!] " + (result.error ?? "Неизвестная ошибка"), true);
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

    private string AnalyzeJavaError(string rawError, string rawDetails = null, string userCode = null)
    {
        if (string.IsNullOrEmpty(rawError)) return "[!] Неизвестная ошибка компиляции";

        // Извлекаем номер строки через поиск текста ошибочной строки в коде пользователя
        string linePrefix = "";
        if (!string.IsNullOrEmpty(rawDetails) && !string.IsNullOrEmpty(userCode))
        {
            // Извлекаем текст ошибочной строки из details
            // Формат: "UserCode.java:10: error: ...\n    Alchemist.addDrop(1)\n    ^"
            var detailLines = rawDetails.Split('\n');
            if (detailLines.Length >= 2)
            {
                string errorCodeLine = detailLines[1].Trim();
                
                // Извлекаем позицию символа из строки с '^'
                string colSuffix = "";
                if (detailLines.Length >= 3)
                {
                    string caretLine = detailLines[2];
                    int caretPos = caretLine.IndexOf('^');
                    int codeIndent = detailLines[1].Length - detailLines[1].TrimStart().Length;
                    if (caretPos >= 0)
                        colSuffix = $", символ {caretPos - codeIndent + 1}";
                }

                // Ищем эту строку в коде игрока
                string[] userLines = userCode.Split('\n');
                for (int i = 0; i < userLines.Length; i++)
                {
                    if (userLines[i].Trim() == errorCodeLine)
                    {
                        linePrefix = $"Строка {i + 1}{colSuffix}: ";
                        break;
                    }
                }
            }
            // Если не нашли через текст — возьмём номер из details с вычитом смещения (7 строк обёртки сервера)
            if (string.IsNullOrEmpty(linePrefix))
            {
                var lineNumMatch = System.Text.RegularExpressions.Regex.Match(
                    rawDetails, @"UserCode\.java:(\d+):");
                if (lineNumMatch.Success && int.TryParse(lineNumMatch.Groups[1].Value, out int compilerLine))
                {
                    int userLine = Mathf.Max(1, compilerLine - 7);
                    linePrefix = $"Строка {userLine}: ";
                }
            }
        }

        // Чистим любой префикс который мог прислать сервер (эмодзи ❌ ✅ или [X] [!])
        // и получаем чистый текст сообщения
        string trimmed = rawError.Trim();
        // Убираем ведущие эмодзи (всё что не ASCII и не кириллица)
        string cleanMsg = System.Text.RegularExpressions.Regex.Replace(
            trimmed, @"^[^\u0020-\u007E\u0400-\u04FF\[]*", "").Trim();
        // Убираем [X] / [!] / [СОВЕТ] в начале если остался
        cleanMsg = System.Text.RegularExpressions.Regex.Replace(
            cleanMsg, @"^\[(?:X|!|СОВЕТ)\]\s*", "").Trim();

        // Если это было «дружелюбное» сообщение от сервера (не сырой Java-вывод)
        if (!cleanMsg.Contains("UserCode.java") && !string.IsNullOrEmpty(cleanMsg))
        {
            return $"[!] {linePrefix}{cleanMsg}";
        }

        // 1. Ошибка переполнения типа (int -> short/byte)
        if (rawError.Contains("possible lossy conversion from int to byte") || 
            rawError.Contains("possible lossy conversion from int to short"))
        {
            return "[СОВЕТ] Число слишком велико для этого типа данных! Оно не влезает в память. Попробуй тип побольше.";
        }

        // 2. Ошибка переполнения int (когда суют в int или забыли L для long)
        if (rawError.Contains("integer number too large"))
        {
            return "[СОВЕТ] Это число слишком большое даже для 'int'. \nЕсли используешь 'long', добавь букву 'L' в конце (например: 9000000000L)!";
        }

        // 3. Ошибка float/double (забыли f)
        if (rawError.Contains("possible lossy conversion from double to float"))
        {
            return "[СОВЕТ] Java считает дробные числа как 'double'. Чтобы использовать 'float', добавь 'f' в конце (например: 0.1f).";
        }

        // 4. Ошибка неправильного имени метода или переменной
        if (rawError.Contains("cannot find symbol"))
        {
            var symMatch = System.Text.RegularExpressions.Regex.Match(rawError, @"symbol:\s+\w+\s+(\w+)");
            if (symMatch.Success)
            {
                string badName = symMatch.Groups[1].Value;
                
                // Список всех известных игровых методов
                string[] knownMethods = { "addDrop", "raiseElevator", "placePlank",
                                          "moveRight", "moveLeft", "moveUp", "moveDown" };
                
                // Ищем ближайший по написанию метод
                string bestMatch = null;
                int bestDist = int.MaxValue;
                foreach (var m in knownMethods)
                {
                    int dist = LevenshteinDistance(badName.ToLower(), m.ToLower());
                    if (dist < bestDist) { bestDist = dist; bestMatch = m; }
                }
                
                // Если похоже (расстояние <= 3) — подсказываем
                string suggestion = (bestDist <= 3 && bestMatch != null)
                    ? $" Ты имел в виду '{bestMatch}'?"
                    : " Проверь правильность написания!";
                    
                return $"[!] '{badName}' не найдено.{suggestion}";
            }
            return "[!] Неизвестное имя. Проверь правильность написания метода или переменной!";
        }

        // 5. Неверное количество аргументов
        if (rawError.Contains("method") && rawError.Contains("cannot be applied to given types"))
        {
            return "[!] Неверные аргументы функции. Проверь, правильно ли ты передаёшь значения в метод!";
        }

        // Если не узнали ошибку — извлекаем номер строки и суть
        // Формат сервера: "UserCode.java:15: error: ';' expected"
        var lineMatch = System.Text.RegularExpressions.Regex.Match(
            rawError, @"UserCode\.java:(\d+):\s*error:\s*(.+)");
        if (lineMatch.Success)
        {
            string lineNum = lineMatch.Groups[1].Value;
            string msg = lineMatch.Groups[2].Value.Trim();
            return $"[!] Строка {lineNum}: {msg}";
        }

        // Совсем неизвестный формат — берём первую строку
        string cleaned = System.Text.RegularExpressions.Regex.Replace(rawError, @"UserCode\.java:\d+: error: ", "");
        cleaned = cleaned.Split('\n')[0].Trim();
        return "[!] " + cleaned;
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

    IEnumerator ExecuteCommandsSequence(GameCommand[] commands, string output)
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

            // codeEditor.AddConsoleLog($"▶️ {cmd.action}({cmd.value})");
            
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
                    // codeEditor.AddConsoleLog($"⚠️ Неизвестная команда: {cmd.action}", true);
                    break;
            }
            
            // ⭐ Быстрая очередь для капель, настраивается в AlchemyLevelManager
            if (cmd.action == "addDrop")
            {
                AlchemyLevelManager almExec = FindFirstObjectByType<AlchemyLevelManager>();
                float dropDelay = almExec != null ? almExec.timeBetweenDrops : 0.15f;
                yield return new WaitForSeconds(dropDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Для уровня с мостом запускаем попытку пройти по мосту в самом конце
        BridgeLevelController levelBridgeController = FindFirstObjectByType<BridgeLevelController>();
        if (levelBridgeController != null)
        {
            yield return levelBridgeController.WalkBridgeAndCheck();
        }
        
        // ⭐ КРИТИЧНО: ВСЕГДА вызываем LevelGameManager
        // чтобы проверить, провалился уровень или нет
        // Задержка небольшая, чтобы успеть доиграть анимации
        // ⭐ ВЫЗЫВАЕМ AlchemyLevelManager с количеством капель
        AlchemyLevelManager almMgr = FindFirstObjectByType<AlchemyLevelManager>();
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (almMgr != null)
        {
            Debug.Log($"[JavaCodeExecutor] 🎯 Вызываем AlchemyLevelManager.OnExecutionFinished() с {dropsCount} каплями");
            almMgr.OnExecutionFinished(dropsCount, output);
        }
        
        if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] 🎯 Вызываем LevelGameManager.OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] LevelGameManager not found!");
        }

        isExecuting = false;
    }

    // Вызывается при ошибке компиляции — ОБВИНЯем ТОЛЬКО LevelGameManager (AlchemyManager не трогаем!)
    void CallExecutionFinishedCompileError()
    {
        isExecuting = false;
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        if (levelManager != null)
            levelManager.OnExecutionFinished();
    }

    void CallExecutionFinished()
    {
        isExecuting = false;
        AlchemyLevelManager almMgr = FindFirstObjectByType<AlchemyLevelManager>();
        LevelGameManager levelManager = FindFirstObjectByType<LevelGameManager>();
        
        if (almMgr != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем AlchemyLevelManager.OnExecutionFinished() с 0 каплями");
            almMgr.OnExecutionFinished(0, "");
        }
        
        if (levelManager != null)
        {
            Debug.Log("[JavaCodeExecutor] ⭐ Вызываем LevelGameManager.OnExecutionFinished()");
            levelManager.OnExecutionFinished();
        }
        else
        {
            Debug.LogWarning("[JavaCodeExecutor] No level manager found!");
        }
    }

    // Алгоритм для вычисления «похожести» двух строк
    private static int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                dp[i, j] = a[i-1] == b[j-1]
                    ? dp[i-1, j-1]
                    : 1 + Math.Min(dp[i-1, j-1], Math.Min(dp[i-1, j], dp[i, j-1]));
        return dp[a.Length, b.Length];
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