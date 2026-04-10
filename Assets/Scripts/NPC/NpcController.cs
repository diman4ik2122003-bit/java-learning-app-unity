using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Универсальный контроллер NPC.
/// Использует GridMovementController для сеточного движения
/// и управляет речевым пузырём над головой персонажа.
///
/// Структура префаба Wizard:
///   Wizard  (компоненты: SpriteRenderer, Animator, Rigidbody2D, CapsuleCollider2D,
///            GridMovementController, NpcController, NpcSequencer)
///   └── SpeechBubble (дочерний GameObject, позиция 0, 1.5, 0)
///       ├── SpriteRenderer  ← спрайт пузыря (9-sliced)
///       └── BubbleText (дочерний GameObject)
///           └── TextMeshPro ← НЕ TextMeshProUGUI, а именно TextMeshPro
/// </summary>
[RequireComponent(typeof(GridMovementController))]
public class NpcController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Заполняется автоматически, если не задан вручную")]
    public GridMovementController movement;

    [Header("Речевой пузырь")]
    [Tooltip("Дочерний объект с пузырём (включается / выключается)")]
    public GameObject speechBubbleRoot;
    [Tooltip("TextMeshPro (world space) с текстом реплики внутри пузыря")]
    public TextMeshPro speechText;

    // ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (movement == null)
            movement = GetComponent<GridMovementController>();

        HideSpeech();
    }

    // ══════════════════════════════════════════
    //  ДВИЖЕНИЕ (возвращает IEnumerator — yield return изнутри секвенсора)
    // ══════════════════════════════════════════

    /// <summary>
    /// Переместить NPC к клетке сетки.
    /// Сначала движение по X, затем по Y.
    /// </summary>
    public IEnumerator MoveToGridAsync(Vector2Int targetGrid)
    {
        Vector2Int current = movement.GetGridPosition();
        int dx = targetGrid.x - current.x;
        int dy = targetGrid.y - current.y;

        if (dx > 0) yield return movement.MoveRight(dx);
        else if (dx < 0) yield return movement.MoveLeft(-dx);

        if (dy > 0) yield return movement.MoveUp(dy);
        else if (dy < 0) yield return movement.MoveDown(-dy);
    }

    /// <summary>
    /// Переместить NPC к мировой позиции (автоматическая конвертация в клетку сетки).
    /// Удобно, если цель — другой GameObject на сцене (котёл, игрок и т.д.).
    /// </summary>
    public IEnumerator MoveToWorldAsync(Vector3 worldPos)
    {
        yield return MoveToGridAsync(movement.WorldToGrid(worldPos));
    }

    // ══════════════════════════════════════════
    //  РЕЧЕВОЙ ПУЗЫРЬ
    // ══════════════════════════════════════════

    [Header("Печатная машинка")]
    [Tooltip("Символов в секунду (0 = мгновенно)")]
    public float typewriterSpeed = 20f;

    private Coroutine _typewriterCoroutine;

    /// <summary>
    /// Показать реплику с эффектом печатной машинки, затем подождать и скрыть пузырь.
    /// displayDuration — сколько секунд пузырь висит ПОСЛЕ того как текст допечатался.
    /// </summary>
    public IEnumerator SayAsync(string text, float displayDuration = 2.5f)
    {
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);

        if (typewriterSpeed <= 0f)
        {
            // Мгновенно
            if (speechText != null) speechText.text = text;
        }
        else
        {
            // Ждём пока допечатается
            yield return TypewriterRoutine(text);
        }

        yield return new WaitForSeconds(displayDuration);
        HideSpeech();
    }

    /// <summary>Показать пузырь с текстом мгновенно (без typewriter, без автоскрытия).</summary>
    public void ShowSpeech(string text)
    {
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
        if (speechText != null)       speechText.text = text;
    }

    /// <summary>Скрыть речевой пузырь.</summary>
    public void HideSpeech()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
    }

    /// <summary>Корутина печатной машинки — открывает символы через TMP visibleCharacters.</summary>
    private IEnumerator TypewriterRoutine(string text)
    {
        if (speechText == null) yield break;

        speechText.text = text;
        speechText.maxVisibleCharacters = 0;

        float delay = 1f / typewriterSpeed;

        for (int i = 0; i <= text.Length; i++)
        {
            speechText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
    }

    // ══════════════════════════════════════════
    //  АНИМАЦИИ
    // ══════════════════════════════════════════

    /// <summary>Вызвать Animator.SetTrigger(name).</summary>
    public void SetAnimTrigger(string triggerName)
    {
        if (movement.animator != null && !string.IsNullOrEmpty(triggerName))
            movement.animator.SetTrigger(triggerName);
    }

    /// <summary>Вызвать Animator.SetBool(name, value).</summary>
    public void SetAnimBool(string paramName, bool value)
    {
        if (movement.animator != null && !string.IsNullOrEmpty(paramName))
            movement.animator.SetBool(paramName, value);
    }

    // ══════════════════════════════════════════
    //  УТИЛИТЫ
    // ══════════════════════════════════════════

    /// <summary>Текущий язык из LocalizationManager ("ru" или "en").</summary>
    public string GetCurrentLang()
    {
        if (LocalizationManager.Instance == null) return "ru";
        return LocalizationManager.Instance.CurrentLang;
    }
}
