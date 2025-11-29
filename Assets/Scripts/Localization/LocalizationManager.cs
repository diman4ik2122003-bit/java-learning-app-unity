// Assets/Scripts/Localization/LocalizationManager.cs
using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    // Текущий язык, стартовое значение не важно — сразу перезапишем
    public string CurrentLang { get; private set; } = "ru";

    public List<string> supportedLanguages = new() { "ru", "en" };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode) || !supportedLanguages.Contains(langCode))
            langCode = "ru";

        if (CurrentLang == langCode)
            return;

        CurrentLang = langCode;
        Debug.Log("[Localization] Language set to: " + CurrentLang);
        OnLanguageChanged?.Invoke(CurrentLang);
    }

    public delegate void LanguageChanged(string langCode);
    public event LanguageChanged OnLanguageChanged;
}
