using System;

[Serializable]
public class IslandProgressDto
{
    public string id;
    public bool unlocked;
    public int completedChallenges;
    public int totalChallenges;
    public int stars;
}

[Serializable]
public class IslandsProgressResponse
{
    public IslandProgressDto[] data;
}
