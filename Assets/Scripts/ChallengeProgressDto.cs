using System;
using UnityEngine;

[Serializable]
public class ChallengeProgressDto
{
    public bool completed;
    public int stars;
    public int totalAttempts;
    public int failedAttempts;
    public int hintsUsed;
    public float bestTime;
}
