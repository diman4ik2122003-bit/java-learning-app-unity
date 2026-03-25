using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeEditor : MonoBehaviour
{
    [SerializeField] private Slider horizontalSlider;

    [Header("UI References")]
    public TMP_InputField codeInput;
    public TMP_Text lineNumbers;
    public ScrollRect scrollRect;

    private RectTransform codeInputRect;
    private RectTransform textRect;
    private RectTransform placeholderRect;
    private RectTransform caretRect;
    private float currentHScrollOffset = 0f;

    private RectTransform _sliderHandleRect;
    private RectTransform _sliderSlideAreaRect;

    private int _lastCaretPos = -1;
    private int _lastSelectFocus = -1;
    private int _lastLineCount = 1;

    void Start()
    {
        codeInputRect = codeInput.GetComponent<RectTransform>();
        textRect = codeInput.textComponent.GetComponent<RectTransform>();
        placeholderRect = codeInput.placeholder.GetComponent<RectTransform>();

        RectTransform textArea = textRect.parent as RectTransform;
        foreach (RectTransform child in textArea)
        {
            if (child != textRect && child != placeholderRect)
            {
                caretRect = child;
                break;
            }
        }

        codeInput.textComponent.enableWordWrapping = false;

        _sliderHandleRect = horizontalSlider.handleRect;
        _sliderSlideAreaRect = _sliderHandleRect.parent as RectTransform;

        horizontalSlider.onValueChanged.AddListener(OnHorizontalScroll);
        lineNumbers.gameObject.SetActive(true);
        codeInput.onValueChanged.AddListener(OnCodeChanged);
        OnCodeChanged(codeInput.text);
    }

    // ── Высота строки ────────────────────────────────────────────────────────
    float GetLineHeight()
    {
        codeInput.textComponent.ForceMeshUpdate();
        var info = codeInput.textComponent.textInfo;
        if (info != null && info.lineCount > 0)
            return info.lineInfo[0].lineHeight;
        // запасной вариант
        return codeInput.textComponent.fontSize * 1.2f;
    }

    // ── Горизонтальный скролл ────────────────────────────────────────────────
    float GetContentWidth()
    {
        codeInput.textComponent.ForceMeshUpdate();
        var textInfo = codeInput.textComponent.textInfo;
        float maxWidth = 0f;
        for (int i = 0; i < textInfo.lineCount; i++)
        {
            float w = textInfo.lineInfo[i].length;
            if (w > maxWidth) maxWidth = w;
        }
        return maxWidth + 20f;
    }

    float GetCaretX()
    {
        int caretPos = codeInput.caretPosition;
        string text = codeInput.text;
        if (string.IsNullOrEmpty(text) || caretPos <= 0) return 0f;

        int lineStart = text.LastIndexOf('\n', caretPos - 1);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        if (caretPos <= lineStart) return 0f;

        string segment = text.Substring(lineStart, caretPos - lineStart);
        return codeInput.textComponent.GetPreferredValues(segment, float.MaxValue, float.MaxValue).x;
    }

    void UpdateHorizontalScrollbar()
    {
        float contentWidth = GetContentWidth();
        float visibleWidth = codeInputRect.rect.width;

        if (contentWidth <= visibleWidth)
        {
            horizontalSlider.gameObject.SetActive(false);
            currentHScrollOffset = 0f;
            return;
        }

        horizontalSlider.gameObject.SetActive(true);

        float maxOffset = Mathf.Max(0f, contentWidth - visibleWidth);
        if (maxOffset > 0f)
            horizontalSlider.SetValueWithoutNotify(Mathf.Clamp01(currentHScrollOffset / maxOffset));
    }

    void OnHorizontalScroll(float value)
    {
        float contentWidth = GetContentWidth();
        float visibleWidth = codeInputRect.rect.width;
        float maxOffset = Mathf.Max(0f, contentWidth - visibleWidth);
        currentHScrollOffset = value * maxOffset;
    }

    void EnsureCaretVisible()
    {
        float caretX = GetCaretX();
        float visibleWidth = codeInputRect.rect.width;
        float margin = 20f;

        if (caretX < currentHScrollOffset + margin)
            currentHScrollOffset = Mathf.Max(0f, caretX - margin);
        else if (caretX > currentHScrollOffset + visibleWidth - margin)
            currentHScrollOffset = caretX - visibleWidth + margin;

        float contentWidth = GetContentWidth();
        float maxOffset = Mathf.Max(0f, contentWidth - visibleWidth);
        if (maxOffset > 0f)
            horizontalSlider.SetValueWithoutNotify(Mathf.Clamp01(currentHScrollOffset / maxOffset));
        else
            horizontalSlider.gameObject.SetActive(false);
    }

    // ── LateUpdate ───────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (codeInput.isFocused)
        {
            int curCaret = codeInput.caretPosition;
            int curFocus = codeInput.selectionFocusPosition;

            if (curCaret != _lastCaretPos || curFocus != _lastSelectFocus)
            {
                _lastCaretPos = curCaret;
                _lastSelectFocus = curFocus;
                EnsureCaretVisible();
            }
        }

        if (textRect != null)
            textRect.anchoredPosition = new Vector2(-currentHScrollOffset, textRect.anchoredPosition.y);
        if (placeholderRect != null)
            placeholderRect.anchoredPosition = new Vector2(-currentHScrollOffset, placeholderRect.anchoredPosition.y);
        if (caretRect != null)
            caretRect.anchoredPosition = new Vector2(-currentHScrollOffset, caretRect.anchoredPosition.y);
    }

    // ── OnCodeChanged ────────────────────────────────────────────────────────
    void OnCodeChanged(string newText)
    {
        lineNumbers.gameObject.SetActive(true);
        UpdateLineNumbers(newText);

        int currentLineCount = string.IsNullOrEmpty(newText) ? 1 : newText.Split('\n').Length;
        bool lineAdded = currentLineCount > _lastLineCount;
        _lastLineCount = currentLineCount;

        StartCoroutine(UpdateScrollbarNextFrame());

        if (lineAdded)
            StartCoroutine(SmartAutoScrollCoroutine());
    }

    IEnumerator UpdateScrollbarNextFrame()
    {
        yield return null;
        UpdateHorizontalScrollbar();
    }

    // ── Вертикальный автоскролл ───────────────────────────────────────────────
    IEnumerator SmartAutoScrollCoroutine()
    {
        // Ждём два кадра — TMP и Layout успевают обновиться
        yield return null;
        yield return null;

        SmartAutoScroll();
    }

    void SmartAutoScroll()
    {
        string text = codeInput.text;
        int caretPos = Mathf.Clamp(codeInput.caretPosition, 0, text.Length);

        // Номер строки каретки (0-based)
        int caretLine = 0;
        for (int i = 0; i < caretPos; i++)
            if (text[i] == '\n') caretLine++;

        float lineH = GetLineHeight();

        // Y строки каретки от верха контента (TMP считает вниз)
        float caretTopY    = caretLine * lineH;
        float caretBottomY = caretTopY + lineH;

        float contentHeight  = scrollRect.content.rect.height;
        float viewportHeight = ((RectTransform)scrollRect.transform).rect.height;

        // Если контент умещается — скроллить некуда
        if (contentHeight <= viewportHeight)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        float scrollable = contentHeight - viewportHeight;

        // normalizedPos = 1 → верх; 0 → низ
        // смещение от верха = (1 - normalizedPos) * scrollable
        float topOffset    = (1f - scrollRect.verticalNormalizedPosition) * scrollable;
        float bottomOffset = topOffset + viewportHeight;

        // Строка уже видна — ничего не делаем
        if (caretTopY >= topOffset && caretBottomY <= bottomOffset)
            return;

        // Скроллим так, чтобы строка каретки оказалась внизу видимой области
        float targetTop = Mathf.Clamp(caretBottomY - viewportHeight, 0f, scrollable);
        scrollRect.verticalNormalizedPosition = 1f - targetTop / scrollable;
    }

    void UpdateLineNumbers(string text)
    {
        string[] lines = string.IsNullOrEmpty(text) ? new[] { "" } : text.Split('\n');
        lineNumbers.text = string.Join("\n", Enumerable.Range(1, lines.Length).Select(i => i.ToString()));
    }

    // ── Update (колесо мыши + Enter) ─────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && codeInput.isFocused)
        {
            currentHScrollOffset = 0f;
            horizontalSlider.SetValueWithoutNotify(0f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    scrollRect.GetComponent<RectTransform>(),
                    Input.mousePosition,
                    Camera.main))
            {
                scrollRect.verticalNormalizedPosition =
                    Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scroll * 3f);
            }
        }
    }
}