using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  Типы действий
// ─────────────────────────────────────────────────────────────────────────────
public enum NpcActionType
{
    MoveTo,           // Переместиться к клетке сетки
    MoveToTransform,  // Переместиться к позиции другого объекта (Transform)
    Say,              // Показать реплику в пузыре
    Wait,             // Подождать N секунд
    PlayAnim,         // Animator.SetTrigger
    SetAnimBool,      // Animator.SetBool
    SetObjectActive,  // Включить / выключить GameObject на сцене
    FireEvent,        // Вызвать произвольный UnityEvent (любая своя логика)
}

// ─────────────────────────────────────────────────────────────────────────────
//  Один шаг последовательности
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class NpcActionStep
{
    [Tooltip("Название шага — только для удобства в Inspector, не влияет на логику")]
    public string stepName = "Step";

    public NpcActionType actionType;

    // ── MoveTo ────────────────────────────────────────
    [Header("MoveTo / MoveToTransform")]
    [Tooltip("Целевая клетка сетки (actionType = MoveTo)")]
    public Vector2Int targetGrid;
    [Tooltip("Целевой объект: NPC встанет на его клетку сетки (actionType = MoveToTransform)")]
    public Transform targetTransform;
    [Tooltip("Скорость ходьбы для этого шага (0 = использовать базовую из GridMovementController)")]
    public float customMoveSpeed = 0f;

    // ── Say ───────────────────────────────────────────
    [Header("Say")]
    [TextArea(1, 4)] public string text_ru;
    [TextArea(1, 4)] public string text_en;
    [Tooltip("Сколько секунд показывать пузырь")]
    public float speechDuration = 2.5f;
    [Header("Voice (Optional)")]
    public AudioClip voiceClip;

    // ── Wait ──────────────────────────────────────────
    [Header("Wait")]
    [Tooltip("Сколько секунд ждать")]
    public float waitDuration = 1f;

    // ── Animation ─────────────────────────────────────
    [Header("Animation")]
    [Tooltip("Имя тригера (actionType = PlayAnim)")]
    public string animTrigger;
    [Tooltip("Имя bool-параметра (actionType = SetAnimBool)")]
    public string animBoolName;
    [Tooltip("Значение bool (actionType = SetAnimBool)")]
    public bool animBoolValue;

    // ── SetObjectActive ───────────────────────────────
    [Header("SetObjectActive")]
    [Tooltip("Объект для вкл/выкл (actionType = SetObjectActive)")]
    public GameObject targetObject;
    [Tooltip("true = включить, false = выключить")]
    public bool setActive = true;

    // ── FireEvent ─────────────────────────────────────
    [Header("FireEvent")]
    [Tooltip("Вызывается при выполнении этого шага")]
    public UnityEvent onExecute;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Секвенсор
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Запускает список действий на NpcController по порядку.
///
/// КАК ИСПОЛЬЗОВАТЬ:
///  1. Добавь NpcSequencer на тот же GameObject, что и NpcController.
///  2. Заполни список steps в Inspector.
///  3. Для запуска вызови Play() из любого места:
///       — из другого MonoBehaviour: GetComponent&lt;NpcSequencer&gt;().Play();
///       — через UnityEvent / Button.onClick
///       — через NpcTriggerZone (отдельный скрипт)
///       — автоматически при старте (флаг playOnStart)
/// </summary>
public class NpcSequencer : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("NpcController, которым управляет этот секвенсор. " +
             "Заполняется автоматически с того же GameObject.")]
    public NpcController npc;

    [Header("Автозапуск")]
    [Tooltip("Запустить последовательность сразу при старте сцены")]
    public bool playOnStart = false;
    [Tooltip("Задержка перед автозапуском (секунды)")]
    public float startDelay = 0f;

    [Header("Последовательность действий")]
    public List<NpcActionStep> steps = new List<NpcActionStep>();

    private Coroutine _running;
    public bool IsPlaying => _running != null;

    // ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (npc == null)
            npc = GetComponent<NpcController>();
    }

    void Start()
    {
        if (playOnStart)
            PlayAfterDelay(startDelay);
    }

    // ══════════════════════════════════════════
    //  Публичное API
    // ══════════════════════════════════════════

    /// <summary>Запустить всю последовательность с начала.</summary>
    public void Play()
    {
        StopSequence();
        _running = StartCoroutine(RunSequence(0));
    }

    /// <summary>Запустить с шага index (0-based).</summary>
    public void PlayFromStep(int index)
    {
        StopSequence();
        _running = StartCoroutine(RunSequence(index));
    }

    /// <summary>Остановить выполнение. Персонаж замирает на текущей позиции.</summary>
    public void StopSequence()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }
        npc?.HideSpeech();
    }

    /// <summary>Запустить последовательность с задержкой.</summary>
    public void PlayAfterDelay(float delay)
    {
        StartCoroutine(DelayedPlay(delay));
    }

    // ══════════════════════════════════════════
    //  Выполнение шагов
    // ══════════════════════════════════════════

    IEnumerator DelayedPlay(float delay)
    {
        if (delay > 0f) 
            yield return new WaitForSeconds(delay);
        else
            yield return null; // Обязательно ждем 1 кадр, чтобы отработали все Start() у других скриптов
            
        Play();
    }

    IEnumerator RunSequence(int startIndex)
    {
        for (int i = startIndex; i < steps.Count; i++)
        {
            if (npc == null) break;
            yield return RunStep(steps[i]);
        }
        _running = null;
    }

    IEnumerator RunStep(NpcActionStep step)
    {
        float originalSpeed = npc.movement != null ? npc.movement.moveSpeed : 0f;
        try
        {
            if (npc.movement != null && step.customMoveSpeed > 0f && 
                (step.actionType == NpcActionType.MoveTo || step.actionType == NpcActionType.MoveToTransform))
            {
                npc.movement.moveSpeed = step.customMoveSpeed;
            }

            switch (step.actionType)
            {
                // ── Движение к клетке ─────────────────────
                case NpcActionType.MoveTo:
                    yield return npc.MoveToGridAsync(step.targetGrid);
                    break;

                // ── Движение к объекту ────────────────────
                case NpcActionType.MoveToTransform:
                    if (step.targetTransform != null)
                        yield return npc.MoveToWorldAsync(step.targetTransform.position);
                    break;

            // ── Реплика ───────────────────────────────
            case NpcActionType.Say:
                string lang = npc.GetCurrentLang();
                string text = (lang == "en" && !string.IsNullOrEmpty(step.text_en))
                    ? step.text_en
                    : step.text_ru;
                yield return npc.SayAsync(text, step.speechDuration, step.voiceClip);
                break;

            // ── Ожидание ──────────────────────────────
            case NpcActionType.Wait:
                yield return new WaitForSeconds(step.waitDuration);
                break;

            // ── Анимация: тригер ──────────────────────
            case NpcActionType.PlayAnim:
                npc.SetAnimTrigger(step.animTrigger);
                break;

            // ── Анимация: bool ────────────────────────
            case NpcActionType.SetAnimBool:
                npc.SetAnimBool(step.animBoolName, step.animBoolValue);
                break;

            // ── Вкл/выкл объект ───────────────────────
            case NpcActionType.SetObjectActive:
                if (step.targetObject != null)
                    step.targetObject.SetActive(step.setActive);
                break;

            // ── Произвольное событие ──────────────────
            case NpcActionType.FireEvent:
                step.onExecute?.Invoke();
                break;
        }
        }
        finally
        {
            if (npc.movement != null && step.customMoveSpeed > 0f && 
                (step.actionType == NpcActionType.MoveTo || step.actionType == NpcActionType.MoveToTransform))
            {
                npc.movement.moveSpeed = originalSpeed;
            }
        }
    }
}
