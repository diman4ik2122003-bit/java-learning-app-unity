using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Переключает Font Asset одновременно на двух TMP_InputField и одном TMP_Text.
/// Кнопки действуют как радио-кнопки: нажатая подсвечивается selectedColor.
/// </summary>
[DisallowMultipleComponent]
public class FontAssetSwitcher : MonoBehaviour
{
    [Header("Target Components")]
    [Tooltip("Первый InputField (например, CodeInput)")]
    [SerializeField] private TMP_InputField inputField1;

    [Tooltip("Второй InputField (например, ConsoleInput)")]
    [SerializeField] private TMP_InputField inputField2;

    [Tooltip("TMP Text (например, LineNumbers)")]
    [SerializeField] private TMP_Text textLabel;

    [Header("Font Assets")]
    [SerializeField] private TMP_FontAsset fontAsset1;
    [SerializeField] private TMP_FontAsset fontAsset2;

    [Header("Toggle Buttons")]
    [Tooltip("Кнопка для выбора первого шрифта")]
    [SerializeField] private Button buttonFont1;

    [Tooltip("Кнопка для выбора второго шрифта")]
    [SerializeField] private Button buttonFont2;

    [Header("Button Visual States")]
    [Tooltip("Цвет кнопки когда она активна (выбрана)")]
    [SerializeField] private Color selectedColor   = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Цвет кнопки когда она неактивна")]
    [SerializeField] private Color deselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Default")]
    [Tooltip("Какой шрифт активен по умолчанию при старте: 1 или 2")]
    [Range(1, 2)]
    [SerializeField] private int defaultFontIndex = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;


    private int _currentFontIndex = 0;


    private void Awake()
    {
        if (buttonFont1 != null) buttonFont1.onClick.AddListener(SelectFont1);
        if (buttonFont2 != null) buttonFont2.onClick.AddListener(SelectFont2);
    }

    private void Start()
    {
        if (defaultFontIndex == 2)
            SelectFont2();
        else
            SelectFont1();
    }

    private void OnDestroy()
    {
        if (buttonFont1 != null) buttonFont1.onClick.RemoveListener(SelectFont1);
        if (buttonFont2 != null) buttonFont2.onClick.RemoveListener(SelectFont2);
    }


    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void SelectFont1()
    {
        if (_currentFontIndex == 1) return;

        if (debugLogs) Debug.Log("[FontAssetSwitcher] Switching to Font Asset 1");

        ApplyFont(fontAsset1);
        _currentFontIndex = 1;
        UpdateButtonVisuals();
    }

    public void SelectFont2()
    {
        if (_currentFontIndex == 2) return;

        if (debugLogs) Debug.Log("[FontAssetSwitcher] Switching to Font Asset 2");

        ApplyFont(fontAsset2);
        _currentFontIndex = 2;
        UpdateButtonVisuals();
    }


    // ========== ПРИВАТНЫЕ МЕТОДЫ ==========

    private void ApplyFont(TMP_FontAsset font)
    {
        if (font == null)
        {
            Debug.LogWarning("[FontAssetSwitcher] ApplyFont: font is null, skipping");
            return;
        }

        if (inputField1 != null)
        {
            inputField1.textComponent.font = font;
            if (inputField1.placeholder is TMP_Text ph1)
                ph1.font = font;
        }
        else if (debugLogs) Debug.LogWarning("[FontAssetSwitcher] inputField1 is not assigned");

        if (inputField2 != null)
        {
            inputField2.textComponent.font = font;
            if (inputField2.placeholder is TMP_Text ph2)
                ph2.font = font;
        }
        else if (debugLogs) Debug.LogWarning("[FontAssetSwitcher] inputField2 is not assigned");

        if (textLabel != null)
            textLabel.font = font;
        else if (debugLogs) Debug.LogWarning("[FontAssetSwitcher] textLabel is not assigned");
    }

    private void UpdateButtonVisuals()
    {
        SetButtonColor(buttonFont1, _currentFontIndex == 1 ? selectedColor : deselectedColor);
        SetButtonColor(buttonFont2, _currentFontIndex == 2 ? selectedColor : deselectedColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;

        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}