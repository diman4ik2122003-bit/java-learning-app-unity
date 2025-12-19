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
    
    [Header("Starting Code")]
    [TextArea(5, 10)]
    public string starterCode = "// Создай переменную distance\n\n// Двигайся на distance метров вправо\n";
    
    [Header("Solution (for progressive hints)")]
    [TextArea(5, 10)]
    public string solutionCode = "int distance = 5;\nPlayer.moveRight(distance);";
    
    [Header("Positions")]
    public Vector3 playerStartPosition = Vector3.zero;
    public Vector3 goalPosition = new Vector3(5, 0, 0);
    
    [Header("Progressive Hints System")]
    public int attemptsBeforeFirstHint = 3;
    
    [Tooltip("Первая подсказка - общая")]
    [TextArea(2, 4)]
    public string hint1 = "💡 Используй переменную для хранения расстояния";
    
    [Tooltip("Вторая подсказка - более конкретная")]
    [TextArea(2, 4)]
    public string hint2 = "💡 Создай переменную: int distance = 5;";
    
    [Tooltip("Третья подсказка - почти решение")]
    [TextArea(2, 4)]
    public string hint3 = "💡 Используй Player.moveRight(distance);";
    
    [Header("Validation (Optional)")]
    public LevelValidation[] validations;
    
    [Header("Legacy Hint (deprecated)")]
    [TextArea(2, 4)]
    public string hint = "💡 Пример:\nint distance = 5;\nPlayer.moveRight(distance);";
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
