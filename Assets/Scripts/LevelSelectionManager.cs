// Assets/Scripts/LevelScripts/LevelSelectionManager.cs
using UnityEngine;

/// <summary>
/// Статическое хранилище для передачи выбранного LevelData между сценами
/// </summary>
public static class LevelSelectionManager
{
    // Хранит выбранный уровень (статический = сохраняется между сценами)
    public static LevelData SelectedLevel { get; set; }
}
