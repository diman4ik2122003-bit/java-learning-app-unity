using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TokenManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterMessageListener();
#endif

    public static TokenManager Instance { get; private set; }

    [Header("Backend base")]
    [SerializeField] private string apiBaseUrl = "https://java-learning-app.onrender.com/api/v1";

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text nicknameText;

    [Header("Binders (optional)")]
    [SerializeField] private StatsPanelBinder statsPanel;
    public AchievementsPanelBinder achievementsPanel;

    [Header("Debug")]
    [SerializeField] private bool logResponses = true;

    [HideInInspector] public string firebaseIdToken;

    public UserProfileResponse profile;
    public UserStatsResponse userStats;
    public AchievementCategoryListResponse achievementCategories;
    public AchievementListResponse achievementsAll;
    public UserAchievementListResponse achievementsMine;
    public bool IsSessionReady { get; private set; }
    public bool IsIslandsReady { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!statsPanel) statsPanel = FindFirstObjectByType<StatsPanelBinder>();
        if (!achievementsPanel) achievementsPanel = FindFirstObjectByType<AchievementsPanelBinder>();
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RegisterMessageListener();
#endif
    }

    /// <summary>Вызывается из JS с idToken Firebase</summary>
    public void ReceiveTokenFromJS(string token)
    {
        firebaseIdToken = token;
        IsSessionReady = false;
        IsIslandsReady = false;
        Debug.Log("[TokenManager] ReceiveTokenFromJS, token length = " + (firebaseIdToken?.Length ?? 0));

        if (JavaJudgeClient.Instance != null)
            JavaJudgeClient.Instance.SetAuthToken(token);

        StartCoroutine(LoadAllSessionData());
    }

    public void RefreshAll()
    {
        if (string.IsNullOrEmpty(firebaseIdToken))
        {
            Debug.LogWarning("[TokenManager] No token yet, cannot refresh.");
            return;
        }
        StartCoroutine(LoadAllSessionData());
    }

    /// <summary>Главная корутина: профиль, статы, ачивки, прогресс уровней</summary>
    private IEnumerator LoadAllSessionData()
    {
        IsSessionReady = false;
        Debug.Log("[TokenManager] LoadAllSessionData START");

        // 1. Профиль
        yield return Get<UserProfileResponse>(
            "/auth/profile",
            ok =>
            {
                profile = ok;
                if (nicknameText)
                    nicknameText.text = profile?.data?.displayName ?? "Нет ника";
            });

        if (profile?.data == null)
            yield break;

        string uid = profile.data.uid;

        // 2. Общая статистика
        yield return Get<UserStatsResponse>(
            "/gamification/stats/" + uid,
            ok => userStats = ok);

        // 3. Категории ачивок
        yield return Get<AchievementCategoryListResponse>(
            "/achievement-categories",
            ok => achievementCategories = ok);

        // 4. Все ачивки
        yield return Get<AchievementListResponse>(
            "/achievements",
            ok => achievementsAll = ok);

        // 5. Мои ачивки
        yield return Get<UserAchievementListResponse>(
            "/achievements/me/all",
            ok => achievementsMine = ok);

        // 6. Прогресс островов
        yield return LoadIslandsProgress(uid);
        IsIslandsReady = true;
        
        // 7. Прогресс уровней (челленджей) для карты островов
        Debug.Log("[TokenManager] Before LoadChallengesProgress");
        yield return LoadChallengesProgress(uid);
        Debug.Log("[TokenManager] After LoadChallengesProgress");

        // 8. Применяем данные к панелям
        if (statsPanel)
            statsPanel.Apply(profile, userStats);

        if (achievementsPanel)
            achievementsPanel.Apply(achievementCategories, achievementsAll, achievementsMine);
    
        IsSessionReady = true;
    }

    /// <summary>Универсальный GET с авторизацией и парсингом JSON через JsonUtility</summary>
    private IEnumerator Get<T>(string path, Action<T> onOk)
    {
        string url = apiBaseUrl.TrimEnd('/') + path;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(firebaseIdToken))
                www.SetRequestHeader("Authorization", "Bearer " + firebaseIdToken);

            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            try
            {
                var obj = JsonUtility.FromJson<T>(www.downloadHandler.text);
                onOk?.Invoke(obj);
            }
            catch (Exception)
            {
                // парс упал — просто игнорим
            }
        }
    }

    public IslandsProgressResponse islandsProgress;

private IEnumerator LoadIslandsProgress(string uid)
{
    string url = apiBaseUrl.TrimEnd('/') + "/progress/islands";

    using (var www = UnityWebRequest.Get(url))
    {
        www.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(firebaseIdToken))
            www.SetRequestHeader("Authorization", "Bearer " + firebaseIdToken);
        www.SetRequestHeader("Accept", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[TokenManager] LoadIslandsProgress error: " + www.error);
            yield break;
        }

        string json = www.downloadHandler.text;
        Debug.Log("[TokenManager] Islands progress body: " + json);

        islandsProgress = JsonUtility.FromJson<IslandsProgressResponse>(json);

        if (islandsProgress == null || islandsProgress.data == null)
        {
            Debug.LogWarning("[TokenManager] IslandsProgressResponse is null or has no data");
        }
        else
        {
            Debug.Log("[TokenManager] Islands count = " + islandsProgress.data.Length);
            foreach (var p in islandsProgress.data)
            {
                Debug.Log($"[TokenManager] Island id={p.id}, unlocked={p.unlocked}, " +
                          $"completed={p.completedChallenges}/{p.totalChallenges}, stars={p.stars}");
            }
        }

        IslandsProgressManager.Instance?.Apply(islandsProgress?.data);
    }
}

    /// <summary>Загрузка прогресса по всем челленджам и передача в LevelProgressManager</summary>
    private IEnumerator LoadChallengesProgress(string uid)
    {
        string path = "/progress/challenges";
        string url = apiBaseUrl.TrimEnd('/') + path;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(firebaseIdToken))
                www.SetRequestHeader("Authorization", "Bearer " + firebaseIdToken);

            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[TokenManager] LoadChallengesProgress error: " + www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            Debug.Log("[TokenManager] Challenges progress body: " + json);

            ChallengesArrayWrapper resp;
            try
            {
                resp = JsonUtility.FromJson<ChallengesArrayWrapper>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[TokenManager] Parse ChallengesProgressResponse error: " + ex);
                yield break;
            }

            if (resp == null || resp.data == null)
            {
                Debug.LogWarning("[TokenManager] ChallengesProgressResponse is null or has no data");
                LevelProgressManager.Instance?.SetChallengesProgress(null);
                yield break;
            }

            // Собираем словарь по id
            var dict = new Dictionary<string, ChallengeProgressDto>();
            foreach (var entry in resp.data)
            {
                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogWarning("[TokenManager] Challenge entry with empty id, skip");
                    continue;
                }

                var dto = new ChallengeProgressDto
                {
                    completed      = entry.completed,
                    stars          = entry.stars,
                    totalAttempts  = entry.totalAttempts,
                    failedAttempts = entry.failedAttempts,
                    hintsUsed      = entry.hintsUsed,
                    bestTime       = entry.bestTime
                };

                dict[entry.id] = dto;
            }

            Debug.Log("[TokenManager] Parsed challenges progress, count=" + dict.Count);
            LevelProgressManager.Instance?.SetChallengesProgress(dict);
            // обновляем остров, если сцена уже загружена
var islandManager = FindFirstObjectByType<IslandSceneManager>();
if (islandManager != null)
{
    Debug.Log("[TokenManager] Refresh island after progress load");
    islandManager.RefreshCurrentIsland();
}

            Debug.Log("[TokenManager] Challenges progress body: " + json);
        Debug.Log("[TokenManager] resp == null: " + (resp == null));
        Debug.Log("[TokenManager] resp.data == null: " + (resp != null && resp.data == null));
        Debug.Log("[TokenManager] resp.data.Length: " + (resp != null && resp.data != null ? resp.data.Length : -1));
        Debug.Log("[TokenManager] dict keys: " + string.Join(",", dict.Keys));
        }
        

    }

    // ==== Вспомогательные DTO для парсинга ответа прогресса ====

    [Serializable]
    private class ChallengesArrayWrapper
    {
        public ChallengeProgressEntry[] data;
    }

    [Serializable]
    private class ChallengeProgressEntry
    {
        public string id;              // challengeId / levelId
        public float bestTime;
        public int codeLines;
        public bool completed;
        public int failedAttempts;
        public long firstCompletedAt;
        public int hintsUsed;
        public long lastCompletedAt;
        public int stars;
        public int totalAttempts;
    }

    // ====== DTO профиля / статы / ачивок ======

    [Serializable]
    public class UserProfileResponse
    {
        public UserProfileData data;
    }

    [Serializable]
    public class UserProfileData
    {
        public string uid;
        public string email;
        public string displayName;
        public string photoURL;
        public string role;
        public UserProfileStats stats;
        public UserProfileGamification gamification;
    }

    [Serializable]
    public class UserProfileStats
    {
        public int level;
        public int xp;
        public int completedLessons;
        public int completedCourses;
        public int achievementsCount;
    }

    [Serializable]
    public class UserProfileGamification
    {
        public int coins;
        public int score;
        public int streakDays;
    }

    [Serializable]
    public class UserStatsResponse
    {
        public UserStatsData data;
    }

    [Serializable]
    public class UserStatsData
    {
        public int xp;
        public int level;
        public string[] badges;
        public int totalChallengesSolved;
        public int totalLessonsCompleted;
        public int currentStreak;
        public int longestStreak;
        public long lastActivityDate;
    }

    [Serializable]
    public class AchievementCategoryListResponse
    {
        public AchievementCategory[] data;
    }

    [Serializable]
    public class AchievementCategory
    {
        public string id;
        public LocalizedString name;
        public int order;
        public string icon;
    }

    [Serializable]
    public class AchievementListResponse
    {
        public Achievement[] data;
    }

    [Serializable]
    public class Achievement
    {
        public string id;
        public string categoryId;
        public LocalizedString title;
        public LocalizedString description;
        public string iconLocked;
        public string iconUnlocked;
        public string conditionType;
        public int conditionValue;
        public int rewardXp;
        public int order;
    }

    [Serializable]
    public class LocalizedString
    {
        public string en;
        public string ru;

        public string GetText(string lang = "ru")
        {
            if (lang == "ru" && !string.IsNullOrEmpty(ru)) return ru;
            if (lang == "en" && !string.IsNullOrEmpty(en)) return en;
            return ru ?? en ?? "";
        }

        public override string ToString()
        {
            return GetText();
        }
    }

    [Serializable]
    public class UserAchievementListResponse
    {
        public UserAchievement[] data;
    }

    [Serializable]
    public class UserAchievement
    {
        public string id;
        public long unlockedAt;
        public bool notified;
    }

}
