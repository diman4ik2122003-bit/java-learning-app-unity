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
    [SerializeField] private CauldronEffects cauldronEffects;
    
    [Header("UI")]
    [SerializeField] private TMP_Text resultText;
    
    [Header("Sequences")]
    public NpcSequencer introSeq;
    public NpcSequencer failSeq;
    public NpcSequencer phase2TransitionSeq;
    public NpcSequencer explosionSeq;
    public NpcSequencer explosionPostSeq;
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
        if (cauldronEffects == null) cauldronEffects = FindFirstObjectByType<CauldronEffects>();

        // ⭐ ПРИВЯЗЫВАЕМ МАГА К ЭФФЕКТАМ КОТЛА
        if (cauldronEffects != null && wizard != null)
        {
            cauldronEffects.wizardAnimator = wizard.GetComponent<Animator>();
        }
    }

    void Start()
    {
        if (introSeq != null)
            introSeq.Play();
    }

    public void DetectDataType(string code)
    {
        if (wizard != null) wizard.HideSpeech(); // Прячем диалоги интро, если они еще висят
        
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

    public void OnExecutionFinished(int dropsAddedThisRun, string rawOutput)
    {
        if (levelCompleted) return;

        // Синхронизируем счетчик
        if (dropsAddedThisRun > 0 && dropsAdded != dropsAddedThisRun)
            dropsAdded = dropsAddedThisRun;

        // ⭐ Ищем ВСЕ числа с точкой и берем ПОСЛЕДНЕЕ (итоговое)
        string valueStr = "1.0";
        var matches = Regex.Matches(rawOutput, @"\d+\.\d+");
        if (matches.Count > 0)
        {
            valueStr = matches[matches.Count - 1].Value;
        }

        if (wizard != null)
            wizard.SetDialogueParams(dropsAdded, valueStr);

        if (currentPhase == 1)
            CheckPhase1(valueStr);
        else if (currentPhase == 2)
            CheckPhase2(valueStr);
    }

    private void CheckPhase1(string valueStr)
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

    private void CheckPhase2(string valueStr)
    {
        if (!usedDouble)
        {
            string msg = "❌ Котёл взорвался! Нужно было использовать тип DOUBLE!";
            codeEditor.AddConsoleLog(msg, true);
            if (resultText != null) resultText.text = msg;
            
            if (explosionSeq != null) explosionSeq.Play();
            
            // ⭐ ЭФФЕКТЫ ВЗРЫВА
            if (cauldronEffects != null)
            {
                cauldronEffects.StartHeating(1.5f); 
                Invoke(nameof(TriggerVisualExplosion), 1.5f);
                // Поучение мага после того, как котел восстановился (1.5 нагрев + 0.7 пауза + 1.2 появление + запас)
                Invoke(nameof(PlayPostExplosionDialogue), 3.8f);
            }
            return;
        }

        if (dropsAdded != requiredDrops)
        {
            codeEditor.AddConsoleLog($"❌ Ошибка: Добавлено {dropsAdded} капель, нужно ровно {requiredDrops}!", true);
            if (failSeq != null) failSeq.Play();
            return;
        }

        // Идеальный успех
        string msgSuccess = $"✅ Концентрация: {valueStr}\n👑 Идеально! Точность соответствует стандартам!";
        codeEditor.AddConsoleLog(msgSuccess);
        
        if (resultText != null) resultText.text = msgSuccess;
        
        if (successSeq != null)
        {
            wizard.SetDialogueParams(dropsAdded, valueStr);
            successSeq.Play();
        }

        levelCompleted = true;
        if (levelGameManager != null) levelGameManager.OnLevelCompleted();
    }

    private void PlayPostExplosionDialogue()
    {
        if (explosionPostSeq != null) explosionPostSeq.Play();
    }

    private void TriggerVisualExplosion()
    {
        if (cauldronEffects != null) cauldronEffects.Explode();
    }
}