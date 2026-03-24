using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeEditor : MonoBehaviour
{
    [SerializeField] private Scrollbar horizontalScrollbar;

    [Header("UI References")]
    public TMP_InputField codeInput;
    public TMP_Text lineNumbers;
    public ScrollRect scrollRect;

    [Header("Auto-scroll settings")]
    public float lineHeight = 20f;

    private RectTransform codeInputRect;
    private RectTransform textRect;
    private RectTransform placeholderRect;
    private RectTransform caretRect;
    private float currentHScrollOffset = 0f;

    private int _lastCaretPos = -1;
    private int _lastSelectFocus = -1;

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

        horizontalScrollbar.onValueChanged.AddListener(OnHorizontalScroll);
        lineNumbers.gameObject.SetActive(true);
        codeInput.onValueChanged.AddListener(OnCodeChanged);
        OnCodeChanged(codeInput.text);
    }

    float GetContentWidth()
    {
        codeInput.textComponent.ForceMeshUpdate();
        var textInfo = codeInput.textComponent.textInfo;
        float maxWidth = 0f;
        for (int i = 0; i < textInfo.lineCount; i++)
        {
            float lineWidth = textInfo.lineInfo[i].length;
            if (lineWidth > maxWidth)
                maxWidth = lineWidth;
        }
        return maxWidth + 20f;
    }

    // Точная X позиция каретки: измеряем ширину текста от начала строки до каретки
    // GetPreferredValues не зависит от координатной системы TMP и всегда возвращает реальные пиксели
    float GetCaretX()
    {
        int caretPos = codeInput.caretPosition;
        string text = codeInput.text;
        if (string.IsNullOrEmpty(text) || caretPos <= 0) return 0f;

        // Ищем начало текущей строки
        int lineStart = text.LastIndexOf('\n', caretPos - 1);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        if (caretPos <= lineStart) return 0f;

        // Ширина текста от начала строки до позиции каретки
        string segment = text.Substring(lineStart, caretPos - lineStart);
        return codeInput.textComponent.GetPreferredValues(segment, float.MaxValue, float.MaxValue).x;
    }

    void UpdateHorizontalScrollbar()
    {
        float contentWidth = GetContentWidth();
        float visibleWidth = codeInputRect.rect.width;

        if (contentWidth <= visibleWidth)
        {
            horizontalScrollbar.gameObject.SetActive(false);
            currentHScrollOffset = 0f;
            return;
        }

        horizontalScrollbar.gameObject.SetActive(true);
        horizontalScrollbar.size = Mathf.Clamp01(visibleWidth / contentWidth);

        float maxOffset = Mathf.Max(0f, contentWidth - visibleWidth);
        if (maxOffset > 0f)
            horizontalScrollbar.SetValueWithoutNotify(currentHScrollOffset / maxOffset);
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

        // Синхронизируем ползунок
        float contentWidth = GetContentWidth();
        float maxOffset = Mathf.Max(0f, contentWidth - visibleWidth);
        if (maxOffset > 0f)
            horizontalScrollbar.SetValueWithoutNotify(currentHScrollOffset / maxOffset);
        else
            horizontalScrollbar.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // Следим за кареткой и выделением — проверяем в LateUpdate чтобы избежать моргания
        // (ForceMeshUpdate в Update сбивал позиции до того как мы их восстанавливали)
        if (codeInput.isFocused)
        {
            int curCaret = codeInput.caretPosition;
            int curFocus = codeInput.selectionFocusPosition; // меняется при выделении мышью

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

    void OnCodeChanged(string newText)
    {
        lineNumbers.gameObject.SetActive(true);
        UpdateLineNumbers(newText);
        StartCoroutine(UpdateScrollbarNextFrame());
        StartCoroutine(SmartAutoScroll());
    }

    IEnumerator UpdateScrollbarNextFrame()
    {
        yield return null;
        UpdateHorizontalScrollbar();
    }

    IEnumerator SmartAutoScroll()
    {
        // 2 кадра — layout должен пересчитать высоту контента после добавления строки
        yield return null;
        yield return null;

        if (codeInput.caretPosition >= codeInput.text.Length - 1)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            yield break;
        }

        float currentPos = scrollRect.verticalNormalizedPosition;
        scrollRect.verticalNormalizedPosition = Mathf.Max(0f, currentPos - 0.05f);
    }

    void UpdateLineNumbers(string text)
    {
        string[] lines = string.IsNullOrEmpty(text) ? new[] { "" } : text.Split('\n');
        lineNumbers.text = string.Join("\n", Enumerable.Range(1, lines.Length).Select(i => i.ToString()));
    }

    void Update()
    {
        // Enter — сбрасываем H скролл чтобы видеть начало новой строки
        if (Input.GetKeyDown(KeyCode.Return) && codeInput.isFocused)
        {
            currentHScrollOffset = 0f;
            horizontalScrollbar.SetValueWithoutNotify(0f);
        }

        // Колёсико мыши — только над CodePanel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    scrollRect.GetComponent<RectTransform>(),
                    Input.mousePosition,
                    Camera.main))
            {
                scrollRect.verticalNormalizedPosition += scroll * 3f;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }
        }
    }
}