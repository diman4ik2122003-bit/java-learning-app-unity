using UnityEngine;
using System.Collections;
using TMPro;

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

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<GridMovementController>();

        HideSpeech();
    }

    // ══════════════════════════════════════════
    //  ДВИЖЕНИЕ
    // ══════════════════════════════════════════

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

    public IEnumerator SayAsync(string text, float displayDuration = 2.5f)
    {
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);

        if (typewriterSpeed <= 0f)
        {
            if (speechText != null) speechText.text = text;
        }
        else
        {
            yield return TypewriterRoutine(text);
        }

        yield return new WaitForSeconds(displayDuration);
        HideSpeech();
    }

    public void ShowSpeech(string text)
    {
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
        if (speechText != null)       speechText.text = text;
    }

    public void HideSpeech()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
    }

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

    public void SetAnimTrigger(string triggerName)
    {
        if (movement.animator != null && !string.IsNullOrEmpty(triggerName))
            movement.animator.SetTrigger(triggerName);
    }

    public void SetAnimBool(string paramName, bool value)
    {
        if (movement.animator != null && !string.IsNullOrEmpty(paramName))
            movement.animator.SetBool(paramName, value);
    }

    // ══════════════════════════════════════════
    //  УТИЛИТЫ
    // ══════════════════════════════════════════

    public string GetCurrentLang()
    {
        if (LocalizationManager.Instance == null) return "ru";
        return LocalizationManager.Instance.CurrentLang;
    }

    /// <summary>
    /// Сброс состояния NPC
    /// </summary>
    public void ResetState()
    {
        HideSpeech();
        
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Cast");
            anim.ResetTrigger("Move");
        }
    }
}