using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PanelSlideController : MonoBehaviour
{
    [Header("Animate THIS (parent panel)")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Positions (anchoredPosition)")]
    [SerializeField] private Vector2 openAnchoredPos;
    [SerializeField] private Vector2 closedAnchoredPos;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float moveDownTime = 1.0f;   // опускание вниз
    [Min(0f)] [SerializeField] private float moveUpTime   = 1.0f;   // подъём вверх

    [Header("Swing after opening")]
    [Min(0f)] [SerializeField] private float swingAmplitude    = 12f;
    [Min(0f)] [SerializeField] private float swingDuration     = 0.85f;
    [Min(0)]  [SerializeField] private int   swingOscillations = 1;

    [Header("Fade (optional)")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaWhenClosed = 0f;

    [Header("Button Sprite")]
    [SerializeField] private Image buttonImage;            // картинка кнопки
    [SerializeField] private Sprite openSprite;             // спрайт со стрелкой вниз
    [SerializeField] private Sprite closeSprite;            // спрайт со стрелкой вверх

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;         // включи в инспекторе


    private Coroutine _routine;
    private bool _isOpened = false;



    private void Reset()
    {
        panelRect        = GetComponent<RectTransform>();
        panelCanvasGroup = GetComponent<CanvasGroup>();

        buttonImage = GetComponent<Image>();

        if (debugLogs)
        {
            Debug.Log("[PanelSlideController] Reset: panelRect = " + (panelRect != null ? "OK" : "null"));
            Debug.Log("[PanelSlideController] Reset: panelCanvasGroup = " + (panelCanvasGroup != null ? "OK" : "null"));
            Debug.Log("[PanelSlideController] Reset: buttonImage = " + (buttonImage != null ? "OK" : "null"));
        }
    }

    private void Awake()
    {
        if (!panelRect) panelRect = GetComponent<RectTransform>();

        if (debugLogs)
        {
            Debug.Log("[PanelSlideController] Awake: panelRect = " + (panelRect != null ? "OK" : "null"));
            Debug.Log("[PanelSlideController] Awake: buttonImage = " + (buttonImage != null ? "OK" : "null"));
        }

        if (panelRect == null)
        {
            Debug.LogError("[PanelSlideController] Нет assigned: panelRect!", this);
            return;
        }

        panelRect.anchoredPosition = closedAnchoredPos;
        _isOpened = false;

        if (panelCanvasGroup) panelCanvasGroup.alpha = alphaWhenClosed;

        UpdateButtonSprite();
    }



    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void OnButtonClicked()
    {
        if (debugLogs)
        {
            Debug.Log("[PanelSlideController] OnButtonClicked called, _isOpened = " + _isOpened + ", _routine = " + (_routine != null).ToString());
        }

        if (_routine != null)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] Routine is running, ignore click");
            return;
        }

        if (_isOpened)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] Going to CLOSE the panel");
            _routine = StartCoroutine(CloseRoutine());
        }
        else
        {
            if (debugLogs) Debug.Log("[PanelSlideController] Going to OPEN the panel");
            _routine = StartCoroutine(OpenRoutine());
        }
    }



    // ========== ПОМОЩЬНИКИ ПО СПРАЙТАМ ==========

    private void UpdateButtonSprite()
    {
        if (buttonImage == null)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] UpdateButtonSprite: buttonImage is null, skip");
            return;
        }

        if (openSprite == null || closeSprite == null)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] UpdateButtonSprite: openSprite or closeSprite is null, skip");
            return;
        }

        if (_isOpened)
        {
            buttonImage.sprite = closeSprite;
            if (debugLogs) Debug.Log("[PanelSlideController] Sprite set to CLOSE (arrow up)");
        }
        else
        {
            buttonImage.sprite = openSprite;
            if (debugLogs) Debug.Log("[PanelSlideController] Sprite set to OPEN (arrow down)");
        }
    }



    // ========== АНИМАЦИИ ==========

    private IEnumerator OpenRoutine()
    {
        if (debugLogs)
            Debug.Log("[PanelSlideController] OpenRoutine: opening panel from=" + panelRect.anchoredPosition + " to=" + openAnchoredPos);

        yield return Move(panelRect.anchoredPosition, openAnchoredPos, moveDownTime, 1f, EaseOutCubic);
        _isOpened = true;

        UpdateButtonSprite();

        yield return Swing(openAnchoredPos);

        if (debugLogs) Debug.Log("[PanelSlideController] OpenRoutine DONE");

        _routine = null;
    }

    private IEnumerator CloseRoutine()
    {
        if (debugLogs)
            Debug.Log("[PanelSlideController] CloseRoutine: closing panel from=" + panelRect.anchoredPosition + " to=" + closedAnchoredPos);

        yield return Move(panelRect.anchoredPosition, closedAnchoredPos, moveUpTime, alphaWhenClosed, EaseInCubic);
        _isOpened = false;

        UpdateButtonSprite();

        if (debugLogs) Debug.Log("[PanelSlideController] CloseRoutine DONE");

        _routine = null;
    }



    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    private IEnumerator Move(Vector2 from, Vector2 to, float duration, float alphaTo, Func<float, float> ease)
    {
        if (duration <= 0.0001f)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] Move: duration too small, instant move");

            panelRect.anchoredPosition = to;
            if (panelCanvasGroup) panelCanvasGroup.alpha = alphaTo;
            yield break;
        }

        float t           = 0f;
        float startAlpha  = panelCanvasGroup ? panelCanvasGroup.alpha : 1f;
        float targetAlpha = panelCanvasGroup ? alphaTo : 1f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            if (t > duration) t = duration;
            float p = t / duration;
            float s = ease != null ? ease(p) : p;

            panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, s);

            if (panelCanvasGroup)
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, s);

            if (debugLogs && Mathf.Approximately(t, 0.1f))
            {
                Debug.Log("[PanelSlideController] Move tick: t=" + t + ", p=" + p + ", s=" + s);
            }

            yield return null;
        }

        panelRect.anchoredPosition = to;
        if (panelCanvasGroup) panelCanvasGroup.alpha = targetAlpha;

        if (debugLogs) Debug.Log("[PanelSlideController] Move DONE: pos=" + panelRect.anchoredPosition + ", alpha=" + (panelCanvasGroup ? panelCanvasGroup.alpha : 1f));
    }

    private IEnumerator Swing(Vector2 basePos)
    {
        if (swingAmplitude <= 0f || swingDuration <= 0f || swingOscillations <= 0)
        {
            if (debugLogs) Debug.Log("[PanelSlideController] Swing skipped: parameters are zero");
            yield break;
        }

        float t = 0f;
        while (t < swingDuration)
        {
            t += Time.unscaledDeltaTime;

            if (t > swingDuration) t = swingDuration;
            float p = t / swingDuration;

            float damp  = 1f - p;
            float phase = p * Mathf.PI * 2f * swingOscillations;
            float y     = Mathf.Sin(phase) * swingAmplitude * damp;

            panelRect.anchoredPosition = basePos + new Vector2(0f, y);

            if (debugLogs && Mathf.Approximately(t, 0.1f))
            {
                Debug.Log("[PanelSlideController] Swing tick: t=" + t + ", y=" + y);
            }

            yield return null;
        }

        panelRect.anchoredPosition = basePos;

        if (debugLogs) Debug.Log("[PanelSlideController] Swing DONE: final pos=" + panelRect.anchoredPosition);
    }

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private float EaseInCubic(float x)  => x * x * x;
}
