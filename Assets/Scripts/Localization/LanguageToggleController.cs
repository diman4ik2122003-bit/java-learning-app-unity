// Assets/Scripts/Localization/LanguageToggleController.cs
using UnityEngine;
using UnityEngine.UI;

public class LanguageToggleController : MonoBehaviour
{
    [SerializeField] private Toggle ruToggle;
    [SerializeField] private Toggle enToggle;

    private void Start()
    {
        // выставляем состояние тоглов по текущему языку
        var lang = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLang
            : "ru";

        ruToggle.isOn = lang == "ru";
        enToggle.isOn = lang == "en";

        // подписки
        ruToggle.onValueChanged.AddListener(OnRuToggleChanged);
        enToggle.onValueChanged.AddListener(OnEnToggleChanged);
    }

    private void OnDestroy()
    {
        ruToggle.onValueChanged.RemoveListener(OnRuToggleChanged);
        enToggle.onValueChanged.RemoveListener(OnEnToggleChanged);
    }

    private void OnRuToggleChanged(bool isOn)
    {
        if (isOn && LocalizationManager.Instance != null)
            LocalizationManager.Instance.SetLanguage("ru");
    }

    private void OnEnToggleChanged(bool isOn)
    {
        if (isOn && LocalizationManager.Instance != null)
            LocalizationManager.Instance.SetLanguage("en");
    }
}
