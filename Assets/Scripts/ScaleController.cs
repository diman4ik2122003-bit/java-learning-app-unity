using UnityEngine;
using TMPro;

public class ScaleController : MonoBehaviour
{
    [Header("UI Targets")]
    [SerializeField] private TMP_InputField inputField1;
    [SerializeField] private TMP_InputField inputField2;
    [SerializeField] private TMP_InputField scaleField;   // это твой "Scale Text" (InputField)
    [SerializeField] private TMP_Text textLabel;          // твой TMP_Text (3‑е место)

    [Header("Scale settings")]
    [SerializeField] private float defaultScalePercent = 100f;
    [SerializeField] private float stepPercent      = 10f;
    [SerializeField] private float minScalePercent  = 50f;
    [SerializeField] private float maxScalePercent  = 200f;

    private float currentScalePercent;

    // базовые размеры шрифта (100%)
    private float baseInput1FontSize;
    private float baseInput2FontSize;
    private float baseTextFontSize;
    private float baseScaleFieldFontSize;

    private void Awake()
    {
        if (inputField1 != null && inputField1.textComponent != null)
            baseInput1FontSize = inputField1.textComponent.fontSize;

        if (inputField2 != null && inputField2.textComponent != null)
            baseInput2FontSize = inputField2.textComponent.fontSize;

        if (textLabel != null)
            baseTextFontSize = textLabel.fontSize;

        if (scaleField != null && scaleField.textComponent != null)
            baseScaleFieldFontSize = scaleField.textComponent.fontSize;

        currentScalePercent = defaultScalePercent;
        ApplyScale();
    }

    public void OnPlusScale()
    {
        currentScalePercent = Mathf.Clamp(currentScalePercent + stepPercent, minScalePercent, maxScalePercent);
        ApplyScale();
    }

    public void OnMinusScale()
    {
        currentScalePercent = Mathf.Clamp(currentScalePercent - stepPercent, minScalePercent, maxScalePercent);
        ApplyScale();
    }

    public void OnResetScale()
    {
        currentScalePercent = defaultScalePercent;
        ApplyScale();
    }

    private void ApplyScale()
    {
        float k = currentScalePercent / 100f;

        if (inputField1 != null && inputField1.textComponent != null)
            inputField1.textComponent.fontSize = baseInput1FontSize * k;

        if (inputField2 != null && inputField2.textComponent != null)
            inputField2.textComponent.fontSize = baseInput2FontSize * k;

        if (textLabel != null)
            textLabel.fontSize = baseTextFontSize * k;

        if (scaleField != null && scaleField.textComponent != null)
        {
            scaleField.textComponent.fontSize = baseScaleFieldFontSize * k;
            scaleField.text = $"{currentScalePercent:0}%";
        }
    }
}
