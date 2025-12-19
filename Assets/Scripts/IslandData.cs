// Assets/Scripts/IslandData.cs
using UnityEngine;

[System.Serializable]
public class LevelButtonData
{
    public Sprite sprite;
    public Vector2 anchoredPosition;
    public LevelData levelData;  // ⭐ НОВОЕ ПОЛЕ - данные уровня
}

[CreateAssetMenu(fileName = "IslandData", menuName = "Game/Island Data")]
public class IslandData : ScriptableObject
{
    public string islandId;          // Например: "1", "2", "forest"
    public Sprite background;        // Фон-спрайт для острова
    public Vector2 levelButtonSize;  // размер всех кнопок уровней для ЭТОГО острова
    public LevelButtonData[] levels;
}
