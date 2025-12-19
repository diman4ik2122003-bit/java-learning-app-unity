using System;
using System.Collections;
using System.Linq;
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
    [SerializeField] private AchievementsPanelBinder achievementsPanel;

    [Header("Debug")]
    [SerializeField] private bool logResponses = false;

    [HideInInspector] public string firebaseIdToken;

    // cached data (как было)
    public UserProfileResponse profile;
    public UserStatsResponse userStats;

    // NEW achievements cache
    public AchievementCategoryListResponse achievementCategories;
    public AchievementListResponse achievementsAll;
    public UserAchievementListResponse achievementsMine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

    // ===== токен из JS =====
    public void ReceiveTokenFromJS(string token)
    {
        firebaseIdToken = token;

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

    private IEnumerator LoadAllSessionData()
    {
        // 1) профиль
        yield return Get<UserProfileResponse>(
            "/auth/profile",
            ok =>
            {
                profile = ok;
                if (nicknameText)
                    nicknameText.text = profile?.data?.displayName ?? "Нет ника";
            });

        if (profile?.data == null) yield break;

        // 2) расширенные статы
        string uid = profile.data.uid;
        yield return Get<UserStatsResponse>(
            "/gamification/stats/" + uid,
            ok => userStats = ok);

        // 3) достижения (новая система)
        yield return Get<AchievementCategoryListResponse>(
            "/achievement-categories",
            ok => achievementCategories = ok);

        yield return Get<AchievementListResponse>(
            "/achievements",
            ok => achievementsAll = ok);

        yield return Get<UserAchievementListResponse>(
            "/achievements/me/all",
            ok => achievementsMine = ok);

        // 4) обновить UI панели (если назначены)
        if (statsPanel) statsPanel.Apply(profile, userStats);

        // ВАЖНО: теперь AchievementsPanelBinder упрощённый — ему не нужны profile/userStats
        if (achievementsPanel)
            achievementsPanel.Apply(achievementCategories, achievementsAll, achievementsMine);
    }

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

            if (logResponses)
            {
                Debug.Log($"GET {path} -> {www.responseCode}");
                Debug.Log(www.downloadHandler.text);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TokenManager] Request error: {www.error} (code {www.responseCode})\n{www.downloadHandler.text}");
                yield break;
            }

            try
            {
                var obj = JsonUtility.FromJson<T>(www.downloadHandler.text);
                onOk?.Invoke(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError("[TokenManager] JSON parse error: " + ex.Message + "\n" + www.downloadHandler.text);
            }
        }
    }

    // ===== DTO (как было) =====
    [Serializable] public class UserProfileResponse { public UserProfileData data; }

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

    [Serializable] public class UserStatsResponse { public UserStatsData data; }

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

    // ===== NEW achievements DTO =====
    [Serializable] public class AchievementCategoryListResponse { public AchievementCategory[] data; }

    [Serializable]
    public class AchievementCategory
    {
        public string id;
        public string name;
        public int order;
        public string icon;
    }

    [Serializable] public class AchievementListResponse { public Achievement[] data; }

    [Serializable]
    public class Achievement
    {
        public string id;
        public string categoryId;
        public string title;
        public string description;

        // В openapi есть iconLocked/iconUnlocked (nullable) [file:659]
        public string iconLocked;
        public string iconUnlocked;
    }

    [Serializable] public class UserAchievementListResponse { public UserAchievement[] data; }

    [Serializable]
    public class UserAchievement
    {
        public string id;
        public long unlockedAt;
        public bool notified;
    }
}
