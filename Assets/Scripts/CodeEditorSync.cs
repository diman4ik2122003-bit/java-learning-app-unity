using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeEditorSync : MonoBehaviour
{
    [Header("Code (ScrollRect + TMP_InputField)")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private ScrollRect codeScroll;

    [Header("Line numbers (NO ScrollRect here)")]
    [SerializeField] private TMP_Text lineNumbersText;
    [SerializeField] private RectTransform lineNumbersContent; // контейнер внутри viewport слева

    [Header("Optional highlight")]
    [SerializeField] private bool highlightActiveLine = true;
    [SerializeField] private Color32 normalColor = new Color32(170, 170, 170, 255);
    [SerializeField] private Color32 activeColor = new Color32(255, 255, 255, 255);

    private int _lastCaret = -1;
    private string _lastCode = null;
    private float _lastCodeScrollY = float.NaN;

    void Reset()
    {
        // удобный дефолт, чтобы не забыть назначить
        highlightActiveLine = true;
    }

    void OnEnable()
    {
        if (codeInput != null)
            codeInput.onValueChanged.AddListener(_ => RebuildLineNumbers()); // [web:124]

        RebuildLineNumbers();
        SyncLineNumbersScroll();
    }

    void OnDisable()
    {
        if (codeInput != null)
            codeInput.onValueChanged.RemoveAllListeners();
    }

    void LateUpdate()
    {
        // 1) синхроним прокрутку каждый кадр (надёжнее, чем onValueChanged,
        // потому что ввод/колесо мыши часто уводит события в TMP_InputField).
        SyncLineNumbersScroll();

        // 2) подсветка / обновление при перемещении каретки
        if (codeInput == null) return;
        int caret = codeInput.caretPosition; // [web:124]
        if (caret != _lastCaret)
        {
            _lastCaret = caret;
            if (highlightActiveLine) RebuildLineNumbers();
        }
    }

    private void SyncLineNumbersScroll()
    {
        if (codeScroll == null || codeScroll.content == null || lineNumbersContent == null) return;

        // ScrollRect двигает content.anchoredPosition (в локальных координатах контента). [web:70]
        float y = codeScroll.content.anchoredPosition.y;

        // Не трогаем, если не изменилось (меньше дерготни).
        if (Mathf.Approximately(y, _lastCodeScrollY)) return;
        _lastCodeScrollY = y;

        // Двигаем контейнер номеров строк, а не сам TMP_Text — так маска не “съедает всё”. [web:319]
        var p = lineNumbersContent.anchoredPosition;
        lineNumbersContent.anchoredPosition = new Vector2(p.x, y);
    }

    private void RebuildLineNumbers()
    {
        if (codeInput == null || lineNumbersText == null) return;

        string code = codeInput.text ?? "";
        if (_lastCode == code && !highlightActiveLine) return;
        _lastCode = code;

        // Для кода правильнее считать строки по '\n' (как в редакторе).
        int lineCount = Mathf.Max(1, code.Split('\n').Length);

        int activeLine = 0;
        if (highlightActiveLine)
            activeLine = GetCaretLineByNewlines(code, codeInput.caretPosition);

        lineNumbersText.text = BuildLineNumbersString(lineCount, activeLine);

        // Если у тебя слева/справа есть fitter/layout, форсим пересчет. [web:215]
        Canvas.ForceUpdateCanvases(); // [web:215]
    }

    private int GetCaretLineByNewlines(string text, int caretPos)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        caretPos = Mathf.Clamp(caretPos, 0, text.Length);

        int line = 0;
        for (int i = 0; i < caretPos; i++)
            if (text[i] == '\n') line++;

        return line; // 0-based
    }

    private string BuildLineNumbersString(int lineCount, int activeLine0)
    {
        var sb = new StringBuilder(lineCount * 8);

        for (int i = 0; i < lineCount; i++)
        {
            int n = i + 1;

            if (highlightActiveLine)
            {
                var c = (i == activeLine0) ? activeColor : normalColor;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(c)).Append(">")
                  .Append(n).Append("</color>");
            }
            else
            {
                sb.Append(n);
            }

            if (i < lineCount - 1) sb.Append('\n');
        }

        return sb.ToString();
    }
}
