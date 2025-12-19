using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeEditorUGUISync : MonoBehaviour
{
    [Header("Code")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private ScrollRect codeScroll;

    [Header("Line numbers")]
    [SerializeField] private TMP_Text lineNumbersText;
    [SerializeField] private ScrollRect lineScroll;

    bool syncing;

    void OnEnable()
    {
        codeInput.onValueChanged.AddListener(_ => UpdateLineNumbers()); // [web:124]
        codeScroll.onValueChanged.AddListener(OnCodeScrolled);         // [web:70]

        UpdateLineNumbers();
        OnCodeScrolled(codeScroll.normalizedPosition);
    }

    void OnDisable()
    {
        codeInput.onValueChanged.RemoveAllListeners();
        codeScroll.onValueChanged.RemoveAllListeners();
    }

    void OnCodeScrolled(Vector2 pos)
    {
        if (syncing) return;
        syncing = true;

        // Копируем позицию 1:1, как в твоём UI Toolkit варианте (только uGUI). [web:91]
        lineScroll.normalizedPosition = pos; // [web:91]

        syncing = false;
    }

    void UpdateLineNumbers()
    {
        string code = codeInput.text ?? "";
        int lineCount = Mathf.Max(1, code.Split('\n').Length);

        var sb = new StringBuilder(lineCount * 4);
        for (int i = 1; i <= lineCount; i++)
        {
            sb.Append(i);
            if (i < lineCount) sb.Append('\n');
        }
        lineNumbersText.text = sb.ToString();
    }
}
