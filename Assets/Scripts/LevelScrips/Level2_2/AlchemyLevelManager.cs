using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class AlchemyLevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NpcController wizard;
    [SerializeField] private CodeEditor codeEditor;
    [SerializeField] private LevelGameManager levelGameManager;
    
    [Header("UI")]
    [SerializeField] private TMP_Text resultText;
    
    [Header("Alchemy Settings")]
    [SerializeField] private int requiredDrops = 10;
    [SerializeField] private float dropValue = 0.1f;
    
    [Header("Phase 1 Messages")]
    [TextArea(2, 4)]
    [SerializeField] private string phase1SuccessMsg = "✅ Концентрация: {0:F10}\n🌾 Для полевых условий сойдёт!";
    
    [Header("Phase 2 Messages")]
    [TextArea(2, 4)]
    [SerializeField] private string phase2SuccessMsg = "✅ Точность: {0:F15}\n👑 Королевская лаборатория одобряет!";
    [TextArea(2, 4)]
    [SerializeField] private string phase2FailMsg = "❌ Недостаточно точный тип данных!\n💥 Котёл взорвался!";
    
    private int currentPhase = 1;
    private int dropsAdded = 0;
    private bool usedDouble = false;
    private bool levelCompleted = false;

    void Awake()
    {
        if (wizard == null)
            wizard = FindFirstObjectByType<NpcController>();
        if (codeEditor == null)
            codeEditor = FindFirstObjectByType<CodeEditor>();
        if (levelGameManager == null)
            levelGameManager = FindFirstObjectByType<LevelGameManager>();
    }

    /// <summary>
    /// Вызывается JavaCodeExecutor когда код выполнен
    /// </summary>
    public void OnExecutionFinished()
    {
        if (levelCompleted) return;

        if (currentPhase == 1)
            CheckPhase1();
        else if (currentPhase == 2)
            CheckPhase2();
    }

    /// <summary>
    /// ФАЗА 1: Полевой эксперимент (float)
    /// </summary>
    private void CheckPhase1()
    {
        if (dropsAdded != requiredDrops)
        {
            codeEditor.AddConsoleLog($"❌ Добавлено {dropsAdded} капель, нужно {requiredDrops}!", true);
            return;
        }

        // Успех фазы 1
        float concentration = dropsAdded * dropValue;
        codeEditor.AddConsoleLog(string.Format(phase1SuccessMsg, concentration));
        
        if (resultText != null)
            resultText.text = string.Format(phase1SuccessMsg, concentration);
        
        if (wizard != null)
            wizard.ShowSpeech("Отлично! Но в королевской лаборатории требуется больше точности...");

        // Переход к фазе 2
        StartCoroutine(TransitionToPhase2());
    }

    /// <summary>
    /// ФАЗА 2: Королевская лаборатория (double)
    /// </summary>
    private void CheckPhase2()
    {
        // Проверка типа данных
        if (!usedDouble)
        {
            codeEditor.AddConsoleLog(phase2FailMsg, true);
            if (resultText != null)
                resultText.text = phase2FailMsg;
            
            if (wizard != null)
                wizard.ShowSpeech("💥 Взрыв! Нужна DOUBLE точность!");
            
            return;
        }

        // Проверка количества капель
        if (dropsAdded != requiredDrops)
        {
            codeEditor.AddConsoleLog($"❌ Добавлено {dropsAdded} капель, нужно {requiredDrops}!", true);
            return;
        }

        // УСПЕХ - LEVEL WIN!
        double concentration = dropsAdded * dropValue;
        codeEditor.AddConsoleLog(string.Format(phase2SuccessMsg, concentration));
        
        if (resultText != null)
            resultText.text = string.Format(phase2SuccessMsg, concentration);
        
        if (wizard != null)
            wizard.ShowSpeech("👑 Идеально! Точность соответствует королевским стандартам!");

        levelCompleted = true;
        
        // Вызываем успех уровня
        if (levelGameManager != null)
            levelGameManager.OnExecutionFinished();
    }

    /// <summary>
    /// Переход между фазами
    /// </summary>
    private IEnumerator TransitionToPhase2()
    {
        yield return new WaitForSeconds(2f);
        
        // Сброс счётчика капель
        dropsAdded = 0;
        usedDouble = false;
        currentPhase = 2;
        
        // Очищаем консоль для новой фазы
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog("\n--- ФАЗА 2: КОРОЛЕВСКАЯ ЛАБОРАТОРИЯ ---\n");
            codeEditor.AddConsoleLog("Используй DOUBLE для точности!");
        }
        
        if (wizard != null)
            wizard.ShowSpeech("Теперь попробуй с double для королевской лаборатории!");
    }

    /// <summary>
    /// Добавить каплю реагента
    /// Вызывается из JavaCodeExecutor
    /// </summary>
    public void OnAddDrop(int count)
    {
        dropsAdded += count;

        // Анимация маги
        if (wizard != null)
        {
            Animator anim = wizard.GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("Cast");
        }

        codeEditor.AddConsoleLog($"💧 +{count} капель. Всего: {dropsAdded}/{requiredDrops}");
    }

    /// <summary>
    /// Определить используется ли double
    /// Вызывается из JavaCodeExecutor перед выполнением
    /// </summary>
    public void DetectDataType(string code)
    {
        // Ищем объявление double переменной
        usedDouble = Regex.IsMatch(code, @"\bdouble\s+\w+\s*=");
        
        Debug.Log($"[AlchemyLevelManager] Фаза {currentPhase}, используется double: {usedDouble}");
    }

/// <summary>
/// Вызывается JavaCodeExecutor с количеством добавленных капель
/// </summary>
public void OnExecutionFinished(int dropsAddedThisRun)
{
    if (levelCompleted) return;

    dropsAdded = dropsAddedThisRun;

    if (currentPhase == 1)
        CheckPhase1();
    else if (currentPhase == 2)
        CheckPhase2();
}
}