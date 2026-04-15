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
    
    [Header("Sequences")]
    public NpcSequencer introSeq;
    public NpcSequencer failSeq;
    public NpcSequencer phase2TransitionSeq;
    public NpcSequencer explosionSeq;
    public NpcSequencer successSeq;

    [Header("Alchemy Settings")]
    [SerializeField] private int requiredDrops = 10;
    
    private int currentPhase = 1;
    private int dropsAdded = 0;
    private bool usedDouble = false;
    private bool levelCompleted = false;

    void Awake()
    {
        if (wizard == null) wizard = FindFirstObjectByType<NpcController>();
        if (codeEditor == null) codeEditor = FindFirstObjectByType<CodeEditor>();
        if (levelGameManager == null) levelGameManager = FindFirstObjectByType<LevelGameManager>();
    }

    void Start()
    {
        if (introSeq != null)
            introSeq.Play();
    }

    public void DetectDataType(string code)
    {
        usedDouble = Regex.IsMatch(code, @"\bdouble\s+\w+\s*=");
        dropsAdded = 0; 
        Debug.Log($"[AlchemyLevelManager] Фаза {currentPhase}, double: {usedDouble}");
    }

    public void OnAddDrop(int count)
    {
        dropsAdded += count;

        if (wizard != null)
        {
            Animator anim = wizard.GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Cast");
        }

        if (codeEditor != null)
            codeEditor.AddConsoleLog($"💧 +{count} капель. Всего: {dropsAdded}/{requiredDrops}");
    }

    public void OnExecutionFinished(int dropsAddedThisRun)
    {
        if (levelCompleted) return;

        // Синхронизируем счетчик на всякий случай
        if (dropsAddedThisRun > 0 && dropsAdded != dropsAddedThisRun)
            dropsAdded = dropsAddedThisRun;

        if (currentPhase == 1)
            CheckPhase1();
        else if (currentPhase == 2)
            CheckPhase2();
    }

    private void CheckPhase1()
    {
        if (dropsAdded != requiredDrops)
        {
            codeEditor.AddConsoleLog($"❌ Ошибка: Добавлено {dropsAdded} капель, нужно ровно {requiredDrops}!", true);
            if (failSeq != null) failSeq.Play();
            return;
        }

        // Успех фазы 1, но симулируем ошибку float
        string floatResult = "1.0000001";
        string msg = $"✅ Концентрация: {floatResult}\n🌾 Для полевых условий сойдёт! Но посмотри на погрешность!";
        codeEditor.AddConsoleLog(msg);
        
        if (resultText != null) resultText.text = msg;
        
        if (phase2TransitionSeq != null) phase2TransitionSeq.Play();

        // Переход во вторую фазу
        currentPhase = 2;
        codeEditor.AddConsoleLog("\n--- ФАЗА 2: БОЕВОЕ ЗЕЛЬЕ ---\nИспользуй тип DOUBLE для абсолютной точности!");
    }

    private void CheckPhase2()
    {
        if (!usedDouble)
        {
            string msg = "❌ Котёл взорвался! Нужно было использовать тип DOUBLE!";
            codeEditor.AddConsoleLog(msg, true);
            if (resultText != null) resultText.text = msg;
            
            if (explosionSeq != null) explosionSeq.Play();
            return;
        }

        if (dropsAdded != requiredDrops)
        {
            codeEditor.AddConsoleLog($"❌ Ошибка: Добавлено {dropsAdded} капель, нужно ровно {requiredDrops}!", true);
            if (failSeq != null) failSeq.Play();
            return;
        }

        // Идеальный успех
        string msgSuccess = $"✅ Концентрация: 1.0\n👑 Идеально! Точность соответствует стандартам!";
        codeEditor.AddConsoleLog(msgSuccess);
        
        if (resultText != null) resultText.text = msgSuccess;
        
        if (successSeq != null) successSeq.Play();

        levelCompleted = true;
        if (levelGameManager != null) levelGameManager.OnExecutionFinished();
    }
}