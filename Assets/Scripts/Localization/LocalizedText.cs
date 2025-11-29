using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string key;
    public LocalizedTextDatabase database;

    TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (!tmpText)
            tmpText = GetComponentInChildren<TMP_Text>();

        Debug.Log($"[LocalizedText] Awake on {gameObject.name}, tmpText={(tmpText ? tmpText.name : "NULL")}");
    }

    void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(LocalizationManager.Instance.CurrentLang);
        }
    }

    void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(LocalizationManager.Instance.CurrentLang);
        }
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }

    void UpdateText(string lang)
    {
        if (!database || tmpText == null)
            return;

        string value = database.Get(key, lang);
        Debug.Log($"[LocalizedText] {gameObject.name} key='{key}' lang='{lang}' => '{value}'");
        tmpText.text = value;
    }
}
