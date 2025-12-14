using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Collections;

public class CodeEditorSync : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField codeInput;
    public TMP_Text displayText;
    public TMP_Text lineNumbers;
    public ScrollRect scrollRect;
    public RectTransform contentRect;

    [Header("Scroll Sync")]
    public RectTransform inputTextArea;  // Text Area внутри CodeInput

    [Header("Layout")]
    public float minHeight = 300f;
    public float padding = 10f;

    [Header("Syntax Highlighting Colors")]
    public string keywordColor = "#569CD6";
    public string numberColor = "#B5CEA8";
    public string commentColor = "#6A9955";

    private string[] keywords = { "moveRight", "moveLeft", "jump", "wait", "repeat", "if" };

    void Start()
    {
        if (codeInput != null)
        {
            codeInput.onValueChanged.AddListener(OnCodeChanged);
            OnCodeChanged(codeInput.text);
        }
    }

    void Update()
    {
        // синхронизируем позицию Text Area с прокруткой Content
        SyncInputScroll();
    }

    void SyncInputScroll()
    {
        if (inputTextArea != null && contentRect != null)
        {
            // двигаем Text Area вместе с Content
            Vector2 contentPos = contentRect.anchoredPosition;
            inputTextArea.anchoredPosition = contentPos;
        }
    }

    void OnCodeChanged(string code)
    {
        if (displayText != null)
        {
            string highlighted = ApplySyntaxHighlight(code);
            displayText.text = highlighted;
            StartCoroutine(UpdateHeightNextFrame());
        }

        UpdateLineNumbers(code);
    }

    IEnumerator UpdateHeightNextFrame()
    {
        yield return null;

        if (displayText != null)
        {
            displayText.ForceMeshUpdate();
            float textHeight = displayText.preferredHeight + padding;
            float newHeight = Mathf.Max(minHeight, textHeight);

            if (contentRect != null)
                contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

            SetElementHeight(displayText.rectTransform, newHeight);
            SetElementHeight(lineNumbers.rectTransform, newHeight);
            
            // синхронизируем высоту Text Area
            if (inputTextArea != null)
                SetElementHeight(inputTextArea, newHeight);
        }

        yield return null;
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void SetElementHeight(RectTransform rt, float height)
    {
        if (rt != null)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    void UpdateLineNumbers(string code)
    {
        if (lineNumbers == null) return;

        int lineCount = code.Split('\n').Length;
        if (lineCount < 1) lineCount = 1;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 1; i <= lineCount; i++)
        {
            sb.Append(i);
            if (i < lineCount) sb.Append('\n');
        }

        lineNumbers.text = sb.ToString();
    }

    string ApplySyntaxHighlight(string code)
    {
        code = Regex.Replace(code, @"(//.*)", $"<color={commentColor}>$1</color>");
        foreach (string keyword in keywords)
        {
            code = Regex.Replace(code, $@"\b({keyword})\b", $"<color={keywordColor}>$1</color>");
        }
        code = Regex.Replace(code, @"\b(\d+\.?\d*)\b", $"<color={numberColor}>$1</color>");
        return code;
    }

    void OnDestroy()
    {
        if (codeInput != null)
            codeInput.onValueChanged.RemoveListener(OnCodeChanged);
    }
}
