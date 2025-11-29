// Assets/Scripts/LocalizedTextDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LocalizedEntry
{
    public string key;      // "main_play", "settings_title" и т.п.
    public string ru;
    public string en;
    // добавишь новые поля, если добавишь языки
}

[CreateAssetMenu(fileName = "LocalizedTextDatabase", menuName = "Game/Localization/Text DB")]
public class LocalizedTextDatabase : ScriptableObject
{
    public List<LocalizedEntry> entries;

    Dictionary<string, LocalizedEntry> dict;

    void OnEnable()
    {
        dict = new Dictionary<string, LocalizedEntry>();
        foreach (var e in entries)
            dict[e.key] = e;
    }

    public string Get(string key, string lang)
    {
        if (dict == null) OnEnable();
        if (!dict.TryGetValue(key, out var entry)) return key;

        return lang switch
        {
            "en" => string.IsNullOrEmpty(entry.en) ? entry.ru : entry.en,
            _    => entry.ru
        };
    }
}
