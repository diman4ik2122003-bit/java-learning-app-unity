using System;

[Serializable]
public class LevelCompletionRequest
{
    public string challengeId;      // ID уровня (например "1-1")
    public int stars;               // 1-3 звезды
    public int completionTime;      // Время в секундах
    public int failedAttempts;      // Количество провалов
    public int hintsUsed;           // Использовано подсказок
    public int codeLines;           // Строк кода
}

[Serializable]
public class LevelCompletionResponse
{
    public LevelCompletionData data;
    public string message;
}

[Serializable]
public class LevelCompletionData
{
    public StatsData stats;
    public int xpGained;
    public Achievement[] achievements;
}

[Serializable]
public class StatsData
{
    public int xp;
    public int level;
    public int challengesSolved;
    public int totalStars;
    public int totalFailedAttempts;
    public int hintsUsed;
    public int totalPlaytimeSeconds;
}

[Serializable]
public class Achievement
{
    public string id;
    public string name;
    public string description;
    public bool isNew;
}
