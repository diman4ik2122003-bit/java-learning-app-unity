// Assets/Scripts/Localization/LocalizationManager.cs
using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    private const string PlayerPrefsKey = "language";

    // Текущий язык
    public string CurrentLang { get; private set; } = "ru";

    public List<string> supportedLanguages = new() { "ru", "en" };

    public delegate void LanguageChanged(string langCode);
    public event LanguageChanged OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // читаем язык из PlayerPrefs
            string saved = PlayerPrefs.GetString(PlayerPrefsKey, "ru");
            SetLanguage(saved, false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // основной метод смены языка
    public void SetLanguage(string langCode)
    {
        SetLanguage(langCode, true);
    }

    private void SetLanguage(string langCode, bool saveToPrefs)
    {
        if (string.IsNullOrEmpty(langCode) || !supportedLanguages.Contains(langCode))
            langCode = "ru";

        if (CurrentLang == langCode)
            return;

        CurrentLang = langCode;
        Debug.Log("[Localization] Language set to: " + CurrentLang);

        if (saveToPrefs)
        {
            PlayerPrefs.SetString(PlayerPrefsKey, CurrentLang);
            PlayerPrefs.Save();
        }

        OnLanguageChanged?.Invoke(CurrentLang);
    }
}
