using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeEditor : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField codeInput;
    public TMP_Text lineNumbers;
    public ScrollRect scrollRect;
    
    [Header("Auto-scroll settings")]
    public float lineHeight = 20f; // высота одной строки в пикселях
    
    void Start()
    {
        lineNumbers.gameObject.SetActive(true);
        codeInput.onValueChanged.AddListener(OnCodeChanged);
        OnCodeChanged(codeInput.text);
    }
    
    void OnCodeChanged(string newText)
    {
        lineNumbers.gameObject.SetActive(true);
        UpdateLineNumbers(newText);
        StartCoroutine(SmartAutoScroll());
    }
    
    System.Collections.IEnumerator SmartAutoScroll()
    {
        yield return null; // ждём обновления layout
        
        // Если каретка в конце текста — прокручиваем вниз
        if (codeInput.caretPosition >= codeInput.text.Length - 1)
        {
            scrollRect.verticalNormalizedPosition = 0f; // до упора вниз
            yield break;
        }
        
        // Подкручиваем на высоту строки
        float currentPos = scrollRect.verticalNormalizedPosition;
        scrollRect.verticalNormalizedPosition = Mathf.Max(0f, currentPos - 0.05f); // 5% вниз
    }
    
    void UpdateLineNumbers(string text)
    {
        string[] lines = (string.IsNullOrEmpty(text) ? new[] {""} : text.Split('\n'));
        lineNumbers.text = string.Join("\n", Enumerable.Range(1, lines.Length).Select(i => i.ToString()));
    }
    
    void Update()
    {
        // Колесико мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            scrollRect.verticalNormalizedPosition += scroll * 3f;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
        
        // Enter в конце = прокрутка вниз
        if (Input.GetKeyDown(KeyCode.Return) && codeInput.isFocused && 
            codeInput.caretPosition >= codeInput.text.Length - 1)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
