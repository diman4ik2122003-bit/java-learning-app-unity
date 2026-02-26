using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class ProfileManager : MonoBehaviour
{
    [Header("UI References - Profile")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI bioText;

    [Header("UI References - Achievements")]
    [SerializeField] private ScrollRect achievementsScrollView;
    [SerializeField] private GameObject achievementRowPrefab;
    [SerializeField] private GameObject achievementItemPrefab;

    [Header("Settings")]
    [SerializeField] private int achievementsPerRow = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string currentProfileUid;
    private string cachedBio;

    private string GetBioPrefix()
    {
        string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
        return lang == "ru" ? "О себе: " : "Bio: ";
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        LoadProfile(null);
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(string newLang)
    {
        if (debugLogs)
            Debug.Log($"[ProfileManager] Language changed to: {newLang}");

        UpdateBioText();
    }

    private void UpdateBioText()
    {
        if (bioText != null && !string.IsNullOrEmpty(cachedBio))
        {
            string bioPrefix = GetBioPrefix();
            bioText.text = bioPrefix + cachedBio;

            if (debugLogs)
                Debug.Log($"[ProfileManager] Bio updated: {bioText.text}");
        }
    }

    /// <summary>
    /// Загружает профиль пользователя
    /// </summary>
    /// <param name="uid">UID пользователя. Если null — загружает свой профиль</param>
    public void LoadProfile(string uid)
    {
        currentProfileUid = uid;

        if (debugLogs)
            Debug.Log($"[ProfileManager] LoadProfile called with uid: {uid ?? "null (my profile)"}");

        if (uid == null)
        {
            LoadMyProfile();
        }
        else
        {
            LoadFriendProfile(uid);
        }
    }

    private void LoadMyProfile()
    {
        if (TokenManager.Instance == null)
        {
            Debug.LogError("[ProfileManager] TokenManager.Instance is null!");
            return;
        }

        if (!TokenManager.Instance.IsSessionReady)
        {
            if (debugLogs)
                Debug.LogWarning("[ProfileManager] Session not ready yet, waiting...");
            
            StartCoroutine(WaitForSessionAndLoad());
            return;
        }

        var profileResponse = TokenManager.Instance.profile;
        var statsResponse = TokenManager.Instance.userStats;
        var achievementsResponse = TokenManager.Instance.achievementsMine;

        if (profileResponse?.data == null)
        {
            Debug.LogError("[ProfileManager] User profile is null!");
            return;
        }

        UpdateUI(profileResponse.data, statsResponse?.data);
        LoadPinnedAchievements(achievementsResponse);
    }

    private IEnumerator WaitForSessionAndLoad()
    {
        while (TokenManager.Instance != null && !TokenManager.Instance.IsSessionReady)
        {
            yield return new WaitForSeconds(0.1f);
        }

        LoadMyProfile();
    }

    private void LoadFriendProfile(string friendUid)
    {
        if (debugLogs)
            Debug.Log($"[ProfileManager] Loading friend profile: {friendUid}");
        
        StartCoroutine(LoadFriendProfileCoroutine(friendUid));
    }

    private IEnumerator LoadFriendProfileCoroutine(string friendUid)
    {
        // TODO: Реализовать загрузку профиля друга через API
        // GET /gamification/stats/{uid}
        // GET /auth/profile/{uid} (если такой эндпоинт есть)
        
        yield return null;
        Debug.LogWarning("[ProfileManager] Friend profile loading not implemented yet!");
    }

    private void UpdateUI(TokenManager.UserProfileData profile, TokenManager.UserStatsData stats)
    {
        if (profile == null)
        {
            Debug.LogError("[ProfileManager] Profile data is null!");
            return;
        }

        // 1. Ник + дискриминатор
        string displayName = profile.displayName ?? "Unknown";
        string discriminator = profile.discriminator ?? "0000";
        string fullName = $"{displayName} #{discriminator}";
        
        nameText.text = fullName;

        if (debugLogs)
            Debug.Log($"[ProfileManager] Name: {fullName}");

        // 2. Уровень
        int level = stats?.level ?? profile.stats?.level ?? 1;
        levelText.text = $"lvl {level}";

        if (debugLogs)
            Debug.Log($"[ProfileManager] Level: {level}");

        // 3. Био
        cachedBio = profile.bio ?? "...";
        UpdateBioText();

        if (debugLogs)
            Debug.Log($"[ProfileManager] Bio cached: {cachedBio}");

        // 4. Аватар
        if (!string.IsNullOrEmpty(profile.photoURL))
        {
            StartCoroutine(LoadAvatar(profile.photoURL));
        }
        else
        {
            if (debugLogs)
                Debug.Log("[ProfileManager] No photo URL, using default");
            
            avatarImage.sprite = null;
        }

        if (debugLogs)
            Debug.Log($"[ProfileManager] Profile loaded successfully");
    }

    private IEnumerator LoadAvatar(string url)
    {
        if (debugLogs)
            Debug.Log($"[ProfileManager] Loading avatar from: {url}");

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            avatarImage.sprite = sprite;
            
            if (debugLogs)
                Debug.Log("[ProfileManager] Avatar loaded successfully");
        }
        else
        {
            Debug.LogError($"[ProfileManager] Failed to load avatar: {request.error}");
            avatarImage.sprite = null;
        }
    }

    /// <summary>
    /// Загружает закрепленные достижения
    /// </summary>
    private void LoadPinnedAchievements(TokenManager.UserAchievementListResponse achievementsResponse)
    {
        if (achievementsScrollView == null)
        {
            Debug.LogError("[ProfileManager] AchievementsScrollView is null!");
            return;
        }

        Transform achievementsContainer = achievementsScrollView.content;
        ClearContainer(achievementsContainer);

        if (achievementsResponse?.data == null || achievementsResponse.data.Length == 0)
        {
            if (debugLogs)
                Debug.Log("[ProfileManager] No achievements data");
            return;
        }

        var pinnedAchievements = achievementsResponse.data
            .Where(a => a.isPinned)
            .OrderBy(a => a.pinOrder)
            .ToList();

        if (pinnedAchievements.Count == 0)
        {
            if (debugLogs)
                Debug.Log("[ProfileManager] No pinned achievements");
            return;
        }

        if (debugLogs)
            Debug.Log($"[ProfileManager] Found {pinnedAchievements.Count} pinned achievements");

        var allAchievements = TokenManager.Instance.achievementsAll?.data;
        if (allAchievements == null)
        {
            Debug.LogError("[ProfileManager] All achievements data is null!");
            return;
        }

        for (int i = 0; i < pinnedAchievements.Count; i += achievementsPerRow)
        {
            var rowAchievements = pinnedAchievements.Skip(i).Take(achievementsPerRow).ToList();
            CreateAchievementRow(achievementsContainer, rowAchievements, allAchievements);
        }
    }

    /// <summary>
    /// Создает строку с достижениями
    /// </summary>
    private void CreateAchievementRow(Transform container, List<TokenManager.UserAchievement> userAchievements, TokenManager.Achievement[] allAchievements)
    {
        if (achievementRowPrefab == null)
        {
            Debug.LogError("[ProfileManager] AchievementRowPrefab is null!");
            return;
        }

        GameObject rowObj = Instantiate(achievementRowPrefab, container);
        Transform rowContainer = rowObj.transform;

        foreach (var userAch in userAchievements)
        {
            var achievement = allAchievements.FirstOrDefault(a => a.id == userAch.id);
            if (achievement == null)
            {
                if (debugLogs)
                    Debug.LogWarning($"[ProfileManager] Achievement {userAch.id} not found");
                continue;
            }

            CreateAchievementItem(rowContainer, achievement, userAch);
        }
    }

    /// <summary>
    /// Создает одну ачивку
    /// </summary>
    private void CreateAchievementItem(Transform parent, TokenManager.Achievement achievement, TokenManager.UserAchievement userAchievement)
    {
        if (achievementItemPrefab == null)
        {
            Debug.LogError("[ProfileManager] AchievementItemPrefab is null!");
            return;
        }

        GameObject itemObj = Instantiate(achievementItemPrefab, parent);

        Image iconImage = itemObj.transform.Find("Icon")?.GetComponent<Image>();
        TextMeshProUGUI titleText = itemObj.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();

        if (titleText != null)
        {
            string currentLang = LocalizationManager.Instance?.CurrentLang ?? "ru";
            titleText.text = achievement.title?.GetText(currentLang) ?? "Achievement";
        }

        if (iconImage != null && !string.IsNullOrEmpty(achievement.iconUnlocked))
        {
            StartCoroutine(LoadAchievementIcon(iconImage, achievement.iconUnlocked));
        }

        if (debugLogs)
            Debug.Log($"[ProfileManager] Created achievement: {achievement.id}");
    }

    private IEnumerator LoadAchievementIcon(Image iconImage, string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            iconImage.sprite = sprite;
        }
        else
        {
            Debug.LogError($"[ProfileManager] Failed to load icon: {request.error}");
        }
    }

    /// <summary>
    /// Очищает контейнер (удаляет всех детей)
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
