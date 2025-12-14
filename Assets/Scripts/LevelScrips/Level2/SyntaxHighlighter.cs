using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class SyntaxHighlighter : MonoBehaviour
{
    public TMP_InputField codeEditor;
    public TMP_Text displayText; // отдельный текст для подсветки (поверх InputField)

    private string[] keywords = { "moveRight", "moveLeft", "jump", "wait", "repeat", "if" };
    private string keywordColor = "#569CD6";  // синий (как в VS Code)
    private string numberColor = "#B5CEA8";   // зелёный
    private string commentColor = "#6A9955";  // тёмно-зелёный
    private string stringColor = "#CE9178";   // оранжевый

    void Start()
    {
        if (codeEditor != null)
        {
            codeEditor.onValueChanged.AddListener(OnCodeChanged);
            OnCodeChanged(codeEditor.text);
        }
    }

    void OnCodeChanged(string code)
    {
        if (displayText == null) return;

        string highlighted = ApplySyntaxHighlight(code);
        displayText.text = highlighted;
    }

    string ApplySyntaxHighlight(string code)
    {
        // комментарии
        code = Regex.Replace(code, @"(//.*)", $"lor={commentColor}>$1</color>");

        // ключевые слова
        foreach (string keyword in keywords)
        {
            code = Regex.Replace(code, $@"\b({keyword})\b", $"lor={keywordColor}>$1</color>");
        }

        // числа
        code = Regex.Replace(code, @"\b(\d+\.?\d*)\b", $"lor={numberColor}>$1</color>");

        return code;
    }

    void OnDestroy()
    {
        if (codeEditor != null)
            codeEditor.onValueChanged.RemoveListener(OnCodeChanged);
    }
}
