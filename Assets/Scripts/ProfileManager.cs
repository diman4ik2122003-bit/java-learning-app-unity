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
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

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

    public void LoadProfile(string uid)
    {
        currentProfileUid = uid;
        if (debugLogs) Debug.Log($"[ProfileManager] LoadProfile called with uid: {uid ?? "null (my profile)"}");

        if (uid == null) LoadMyProfile();
        else LoadFriendProfile(uid);
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

        // Передаём оба списка в биндер — он сам всё отрисует
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

    private void LoadFriendProfile(string friendUid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Loading friend profile: {friendUid}");
        StartCoroutine(LoadFriendProfileCoroutine(friendUid));
    }

    private IEnumerator LoadFriendProfileCoroutine(string friendUid)
    {
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

        string displayName  = profile.displayName ?? "Unknown";
        string discriminator = profile.discriminator ?? "0000";
        nameText.text = $"{displayName} #{discriminator}";
        if (debugLogs) Debug.Log($"[ProfileManager] Name: {nameText.text}");

        int level = stats?.level ?? profile.stats?.level ?? 1;
        levelText.text = $"lvl {level}";
        if (debugLogs) Debug.Log($"[ProfileManager] Level: {level}");

        cachedBio = profile.bio ?? "...";
        UpdateBioText();
        if (debugLogs) Debug.Log($"[ProfileManager] Bio cached: {cachedBio}");

        if (!string.IsNullOrEmpty(profile.photoURL))
            StartCoroutine(LoadAvatar(profile.photoURL));
        else
            avatarImage.sprite = null;

        if (debugLogs) Debug.Log("[ProfileManager] Profile loaded successfully");
    }

    private IEnumerator LoadAvatar(string url)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Loading avatar from: {url}");

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
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
            avatarImage.sprite = null;
        }
    }
}
