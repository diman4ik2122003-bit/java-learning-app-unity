using System.Collections.Generic;
using UnityEngine;

public class IslandsProgressManager : MonoBehaviour
{
    public static IslandsProgressManager Instance { get; private set; }

    private readonly Dictionary<string, IslandProgressDto> _islands =
        new Dictionary<string, IslandProgressDto>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Apply(IslandProgressDto[] data)
    {
        _islands.Clear();
        if (data == null) return;

        foreach (var p in data)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;
            _islands[p.id] = p;
        }

        Debug.Log("[IslandsProgressManager] Apply: count=" + _islands.Count);
    }

    public bool IsUnlocked(string islandId)
    {
        if (string.IsNullOrEmpty(islandId)) return false;
        // первый остров всегда открыт
        if (islandId == "1") return true;

        return _islands.TryGetValue(islandId, out var p) && p.unlocked;
    }
}
