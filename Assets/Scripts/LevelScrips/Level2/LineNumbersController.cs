using UnityEngine;
using TMPro;

public class LineNumbersController : MonoBehaviour
{
    public TMP_InputField codeEditor;
    public TMP_Text lineNumbersText;

    void Start()
    {
        if (codeEditor != null)
        {
            codeEditor.onValueChanged.AddListener(UpdateLineNumbers);
            UpdateLineNumbers(codeEditor.text);
        }
    }

    void UpdateLineNumbers(string code)
    {
        if (lineNumbersText == null) return;

        int lineCount = code.Split('\n').Length;
        string numbers = "";
        
        for (int i = 1; i <= lineCount; i++)
        {
            numbers += i.ToString();
            if (i < lineCount)
                numbers += "\n";
        }
        
        lineNumbersText.text = numbers;
    }

    void OnDestroy()
    {
        if (codeEditor != null)
            codeEditor.onValueChanged.RemoveListener(UpdateLineNumbers);
    }
}
