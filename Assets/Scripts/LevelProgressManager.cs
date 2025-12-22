using System.Collections.Generic;
using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    // levelId/challengeId -> progress
    private Dictionary<string, ChallengeProgressDto> _challenges =
        new Dictionary<string, ChallengeProgressDto>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LevelProgressManager] Awake, instance created");
    }

    public void SetChallengesProgress(Dictionary<string, ChallengeProgressDto> dict)
    {
        _challenges = dict ?? new Dictionary<string, ChallengeProgressDto>();
        Debug.Log($"[LevelProgressManager] SetChallengesProgress: count={_challenges.Count}");
    }

    public bool TryGet(string levelId, out ChallengeProgressDto progress)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[LevelProgressManager] TryGet called with empty levelId");
            progress = null;
            return false;
        }

        bool found = _challenges.TryGetValue(levelId, out progress);
Debug.Log($"[LevelProgressManager] TryGet: levelId={levelId}, found={found}. Keys={string.Join(",", _challenges.Keys)}");
        return found;
    }

    public int GetStars(string levelId)
    {
        return TryGet(levelId, out var p) ? p.stars : 0;
    }

    public bool IsCompleted(string levelId)
    {
        return TryGet(levelId, out var p) && p.completed;
    }
}
