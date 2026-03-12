using System.Collections;
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

    [Header("Pinned Achievements")]
    [SerializeField] private PinnedAchievementsPanelBinder pinnedBinder;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string currentProfileUid = "NOT_SET";
    private string cachedBio;

    private string GetBioPrefix()
    {
        string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
        return lang == "ru" ? "О себе: " : "Bio: ";
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

        // При открытии панели — сбрасываем uid чтобы всегда грузить мой профиль заново
        currentProfileUid = "NOT_SET";
        LoadProfile(null);
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string newLang)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Language changed to: {newLang}");
        UpdateBioText();
    }

    private void UpdateBioText()
    {
        if (bioText != null && !string.IsNullOrEmpty(cachedBio))
        {
            bioText.text = GetBioPrefix() + cachedBio;
            if (debugLogs) Debug.Log($"[ProfileManager] Bio updated: {bioText.text}");
        }
    }

    /// <summary>
    /// Сбросить guard чтобы следующий LoadProfile гарантированно перезагрузил данные.
    /// Вызывается из FriendsPanelBinder перед LoadProfile(friendUid).
    /// </summary>
    public void ResetCurrentProfile()
    {
        currentProfileUid = "NOT_SET";
        if (debugLogs) Debug.Log("[ProfileManager] ResetCurrentProfile called");
    }

    public void LoadProfile(string uid)
    {
        // Защита от повторной загрузки того же профиля
        if (uid == currentProfileUid)
        {
            if (debugLogs) Debug.Log($"[ProfileManager] Profile '{uid}' already loaded, skipping");
            return;
        }

        currentProfileUid = uid;

        if (debugLogs) Debug.Log($"[ProfileManager] LoadProfile called with uid: {uid ?? "null (my profile)"}");

        if (uid == null) LoadMyProfile();
        else             StartCoroutine(LoadFriendProfileCoroutine(uid));
    }

    // ========== МОЙ ПРОФИЛЬ ==========

    private void LoadMyProfile()
    {
        if (TokenManager.Instance == null)
        {
            Debug.LogError("[ProfileManager] TokenManager.Instance is null!");
            return;
        }

        if (!TokenManager.Instance.IsSessionReady)
        {
            if (debugLogs) Debug.LogWarning("[ProfileManager] Session not ready yet, waiting...");
            StartCoroutine(WaitForSessionAndLoad());
            return;
        }

        var profileResponse = TokenManager.Instance.profile;
        var statsResponse   = TokenManager.Instance.userStats;

        if (profileResponse?.data == null)
        {
            Debug.LogError("[ProfileManager] User profile is null!");
            return;
        }

        UpdateUI(profileResponse.data, statsResponse?.data);

        pinnedBinder?.Apply(
            TokenManager.Instance.achievementsAll,
            TokenManager.Instance.achievementsMine
        );
    }

    private IEnumerator WaitForSessionAndLoad()
    {
        while (TokenManager.Instance != null && !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        LoadMyProfile();
    }

    // ========== ПРОФИЛЬ ДРУГА ==========

    private IEnumerator LoadFriendProfileCoroutine(string friendUid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Loading friend profile: {friendUid}");

        // Ждём сессию
        while (TokenManager.Instance != null && !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        if (TokenManager.Instance == null)
        {
            Debug.LogError("[ProfileManager] TokenManager.Instance is null!");
            yield break;
        }

        // Ищем в кеше друзей
        TokenManager.FriendData friendData = FindFriendInCache(friendUid);

        if (friendData != null)
        {
            if (debugLogs) Debug.Log($"[ProfileManager] Found friend {friendUid} in cache");

            var profile = new TokenManager.UserProfileData
            {
                uid           = friendData.uid,
                displayName   = friendData.displayName,
                discriminator = friendData.discriminator,
                bio           = friendData.bio,
                photoURL      = friendData.photoURL,
                stats         = new TokenManager.UserProfileStats { level = friendData.level }
            };

            UpdateUI(profile, null);

            // Ачивки друга не показываем — очищаем панель
            // pinnedBinder?.Clear(); // раскомментируй если добавишь метод Clear()

            if (debugLogs) Debug.Log("[ProfileManager] Friend profile loaded from cache");
            yield break;
        }

        // Не нашли в кеше — показываем заглушку
        Debug.LogWarning($"[ProfileManager] Friend {friendUid} not found in cachedFriends, showing fallback");
        ShowFallbackProfile(friendUid);
    }

    private TokenManager.FriendData FindFriendInCache(string friendUid)
    {
        var friendsResp = TokenManager.Instance?.cachedFriends;
        if (friendsResp?.data == null) return null;

        foreach (var f in friendsResp.data)
            if (f.uid == friendUid) return f;

        return null;
    }

    private void ShowFallbackProfile(string uid)
    {
        if (nameText    != null) nameText.text         = "Unknown #0000";
        if (levelText   != null) levelText.text        = "lvl ?";
        if (bioText     != null) bioText.text          = "";
        if (avatarImage != null) avatarImage.sprite    = null;
        Debug.LogWarning($"[ProfileManager] Fallback profile shown for uid={uid}");
    }

    // ========== UI ==========

    private void UpdateUI(TokenManager.UserProfileData profile, TokenManager.UserStatsData stats)
    {
        if (profile == null)
        {
            Debug.LogError("[ProfileManager] Profile data is null!");
            return;
        }

        string displayName   = profile.displayName   ?? "Unknown";
        string discriminator = profile.discriminator ?? "0000";

        if (nameText  != null) nameText.text  = $"{displayName} #{discriminator}";
        if (debugLogs) Debug.Log($"[ProfileManager] Name: {nameText?.text}");

        int level = stats?.level ?? profile.stats?.level ?? 1;
        if (levelText != null) levelText.text = $"lvl {level}";
        if (debugLogs) Debug.Log($"[ProfileManager] Level: {level}");

        cachedBio = profile.bio ?? "...";
        UpdateBioText();
        if (debugLogs) Debug.Log($"[ProfileManager] Bio cached: {cachedBio}");

        if (!string.IsNullOrEmpty(profile.photoURL))
            StartCoroutine(LoadAvatar(profile.photoURL));
        else if (avatarImage != null)
            avatarImage.sprite = null;

        if (debugLogs) Debug.Log("[ProfileManager] UpdateUI done");
    }

    private IEnumerator LoadAvatar(string url)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Loading avatar from: {url}");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (avatarImage != null)
                    avatarImage.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                if (debugLogs) Debug.Log("[ProfileManager] Avatar loaded successfully");
            }
            else
            {
                Debug.LogError($"[ProfileManager] Failed to load avatar: {request.error}");
                if (avatarImage != null) avatarImage.sprite = null;
            }
        }
    }
}
