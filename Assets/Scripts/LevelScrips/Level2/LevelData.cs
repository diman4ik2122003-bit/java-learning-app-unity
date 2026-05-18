using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelId = "1-1";
    public string groupName = "Переменные";
     public string groupName_en;
    public string levelName = "Первая переменная";
    public string levelName_en;

    [Header("Level Scene")]
    public GameObject levelPrefab; // Prefab со всей сценой уровня
    [Tooltip("Опционально: Имя сцены для этого уровня. Если пусто, имя будет определено автоматически по имени ассета.")]
    public string sceneName;
    
    [Header("Description")]
    [TextArea(3, 6)]
    public string description = "Создай переменную distance и используй её в Player.moveRight()";
    [TextArea(3, 6)]
    public string description_en;

    [Header("Starting Code")]
    [TextArea(5, 10)]
    public string starterCode = "// Создай переменную distance\n\n// Двигайся на distance метров вправо\n";
    [TextArea(5, 10)] public string starterCode_en;

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
    
    [TextArea(2, 4)] public string hint1_en;
    [TextArea(2, 4)] public string hint2_en;
    [TextArea(2, 4)] public string hint3_en;

    [Header("Validation (Optional)")]
    public LevelValidation[] validations;

    [Header("Rating Conditions")]
    [Tooltip("Время в секундах, за которое даются 3 звезды")]
    public int timeFor3Stars = 300;
    [Tooltip("Макс. количество ошибок для 3 звезд")]
    public int attemptsFor3Stars = 2;
    [Tooltip("Макс. количество ошибок для 2 звезд")]
    public int attemptsFor2Stars = 5;
    
    [Header("Contextual Hints (New System)")]
    public List<LevelStep> steps = new List<LevelStep>();

    [Header("Legacy Hint (deprecated)")]
    [TextArea(2, 4)]
    public string hint = "💡 Пример:\nint distance = 5;\nPlayer.moveRight(distance);";
}

[Serializable]
public class LevelStep
{
    public string stepName; // Название шага (например, "Лифт 1")
    [TextArea(3, 10)]
    public List<string> hints = new List<string>(); // Список подсказок для этого шага
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
