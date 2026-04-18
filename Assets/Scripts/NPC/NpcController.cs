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

    private AudioSource _audioSource;
    private object[] _dialogueParams; // Параметры для подстановки в текст {0}, {1}...

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<GridMovementController>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // Делаем звук 3D (тише, если игрок далеко)
        }

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

    public IEnumerator SayAsync(string text, float displayDuration = 2.5f, AudioClip voiceClip = null)
    {
        // ⭐ ПОДСТАНОВКА ПАРАМЕТРОВ
        try {
            if (_dialogueParams != null && _dialogueParams.Length > 0)
            {
                text = string.Format(text, _dialogueParams);
            }
        } catch (System.Exception e) {
            Debug.LogWarning($"[NpcController] Ошибка форматирования текста: {e.Message}");
        }

        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);

        if (voiceClip != null && _audioSource != null)
        {
            _audioSource.clip = voiceClip;
            _audioSource.Play();
        }

        if (typewriterSpeed <= 0f)
        {
            if (speechText != null) speechText.text = text;
        }
        else
        {
            yield return TypewriterRoutine(text);
        }

        // Небольшая задержка, чтобы случайный клик от пропуска текста не закрыл его сразу
        yield return new WaitForSeconds(0.1f);

        // Ждем клика игрока для продолжения (как в RPG)
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        // ⭐ Ждем, пока игрок ОТПУСТИТ кнопку, чтобы клик не перешел на следующее сообщение
        yield return new WaitUntil(() => !Input.GetMouseButton(0));

        HideSpeech();
    }

    public void ShowSpeech(string text)
    {
        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
        if (speechText != null)       speechText.text = text;
    }

    public void HideSpeech()
    {
        if (_audioSource != null) _audioSource.Stop();

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
        int totalChars = text.Length;
        speechText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalChars; i++)
        {
            if (Input.GetMouseButtonDown(0))
            {
                speechText.maxVisibleCharacters = totalChars;
                yield break;
            }

            speechText.maxVisibleCharacters = i;
            
            float timer = 1f / typewriterSpeed;
            while (timer > 0)
            {
                // Проверка клика внутри ожидания между буквами
                if (Input.GetMouseButtonDown(0))
                {
                    speechText.maxVisibleCharacters = totalChars;
                    yield break;
                }
                timer -= Time.deltaTime;
                yield return null;
            }
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

    public void SetDialogueParams(params object[] args)
    {
        _dialogueParams = args;
    }
}