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

    [Header("Auto-scroll settings")]
    public float lineHeight = 20f;

    private RectTransform codeInputRect;
    private RectTransform textRect;
    private RectTransform placeholderRect;
    private RectTransform caretRect;
    private float currentHScrollOffset = 0f;

    private RectTransform _sliderHandleRect;
    private RectTransform _sliderSlideAreaRect;

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

        // Slider горизонтальный — rect.width трека, sizeDelta.x ручки
        _sliderHandleRect = horizontalSlider.handleRect;
        _sliderSlideAreaRect = _sliderHandleRect.parent as RectTransform;

        horizontalSlider.onValueChanged.AddListener(OnHorizontalScroll);
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

        // Чистый горизонтальный Slider: ширина трека = rect.width, ручка — sizeDelta.x
        float trackWidth = _sliderSlideAreaRect.rect.width;
        if (trackWidth > 0f)
        {
            float ratio = Mathf.Clamp01(visibleWidth / contentWidth);
            _sliderHandleRect.sizeDelta = new Vector2(
                ratio * trackWidth,
                _sliderHandleRect.sizeDelta.y
            );
        }

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
                scrollRect.verticalNormalizedPosition += scroll * 3f;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }
        }
    }
}