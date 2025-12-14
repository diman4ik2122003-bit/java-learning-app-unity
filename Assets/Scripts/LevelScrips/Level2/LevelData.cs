using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelId = "1-1";
    public string groupName = "Переменные";
    public string levelName = "Первая переменная";
    
    [Header("Level Scene")]
    public GameObject levelPrefab; // Prefab со всей сценой уровня
    
    [Header("Description")]
    [TextArea(3, 6)]
    public string description = "Создай переменную distance и используй её в Player.moveRight()";
    
    [TextArea(2, 4)]
    public string hint = "💡 Пример:\nint distance = 5;\nPlayer.moveRight(distance);";
    
    [Header("Starting Code")]
    [TextArea(5, 10)]
    public string starterCode = "// Создай переменную distance\n\n// Двигайся на distance метров вправо\n";
    
    [Header("Positions")]
    public Vector3 playerStartPosition = Vector3.zero;
    public Vector3 goalPosition = new Vector3(5, 0, 0);
    
    [Header("Validation")]
    public LevelValidation[] validations;
}

[Serializable]
public class LevelValidation
{
    [Header("Pattern")]
    public string pattern; // regex паттерн
    public string hint; // подсказка при несоблюдении
    
    [Header("Examples")]
    [TextArea(2, 3)]
    public string validExample = "int distance = 5;";
    [TextArea(2, 3)]
    public string invalidExample = "Player.moveRight(5); // напрямую число";
}



// using UnityEngine;

// [CreateAssetMenu(fileName = "Level", menuName = "Game/LevelData")]
// public class LevelData : ScriptableObject
// {
//     public string levelNumber = "1";
//     public string title = "Уровень 1: Первые шаги";
    
//     [TextArea(3, 10)]
//     public string description = "Научись двигать персонажа вправо.\n\nИспользуй команду:\nmoveRight(расстояние)";
    
//     [TextArea(5, 15)]
//     public string starterCode = "// Твой код здесь\nmoveRight(2)\njump(5)\nmoveRight(2)";
    
//     public Vector2 playerStartPosition = new Vector2(-4, -2);
//     public Vector2 goalPosition = new Vector2(4, -2);
// }
