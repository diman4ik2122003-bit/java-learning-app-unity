using System.Collections;
using UnityEngine;

public class CodeExecutor : MonoBehaviour
{
    public static CodeExecutor Instance;

    private Coroutine currentExecution;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Execute(string code, PlayerController player)
    {
        // останавливаем предыдущее выполнение, если есть
        if (currentExecution != null)
            StopCoroutine(currentExecution);

        currentExecution = StartCoroutine(ParseAndRun(code, player));
    }

    public void StopExecution()
    {
        if (currentExecution != null)
        {
            StopCoroutine(currentExecution);
            currentExecution = null;
        }
    }

    IEnumerator ParseAndRun(string code, PlayerController player)
    {
        ConsoleController.Log("▶️ Запуск кода...");
        
        string[] lines = code.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                continue;

            ConsoleController.Log($"Выполняю: {trimmed}");

            if (trimmed.StartsWith("moveRight"))
            {
                float dist = ParseFloat(trimmed);
                player.MoveRight(dist);
                yield return new WaitForSeconds(0.3f);
            }
            else if (trimmed.StartsWith("jump"))
            {
                float force = ParseFloat(trimmed);
                player.Jump(force);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                ConsoleController.LogError($"❌ Неизвестная команда: {trimmed}");
            }
        }

        ConsoleController.Log("✅ Выполнение завершено");
        currentExecution = null;
    }

    float ParseFloat(string line)
    {
        // парсинг: "moveRight(2.5)" → 2.5
        int start = line.IndexOf('(') + 1;
        int end = line.IndexOf(')');
        
        if (start > 0 && end > start)
        {
            string numStr = line.Substring(start, end - start).Trim();
            if (float.TryParse(numStr, out float result))
                return result;
        }
        
        Debug.LogWarning("[CodeExecutor] Не удалось распарсить число из: " + line);
        return 0f;
    }
}
