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

    [Header("Friend Profile Extras")]
    [SerializeField] private GameObject tabsContainer;
    [SerializeField] private FriendsPanelBinder friendsPanelBinder;

    [Header("Navigation")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private BoardSlideSwitcher boardSlideSwitcher;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string currentProfileUid = "NOT_SET";
    private string cachedBio;

    private string _pendingFriendUid = null;
    private bool   _hasPendingFriend = false;

    private string GetBioPrefix()
    {
        string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
        return lang == "ru" ? "О себе: " : "Bio: ";
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

        if (_hasPendingFriend)
        {
            _hasPendingFriend = false;
            currentProfileUid = "NOT_SET";
            string uid        = _pendingFriendUid;
            _pendingFriendUid = null;
            if (debugLogs) Debug.Log($"[ProfileManager] OnEnable: loading pending friend uid={uid}");
            LoadProfile(uid);
        }
        else
        {
            if (debugLogs) Debug.Log("[ProfileManager] OnEnable: loading my profile");
            currentProfileUid = "NOT_SET";
            LoadProfile(null);
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;

        _hasPendingFriend = false;
        _pendingFriendUid = null;
    }

    private void OnLanguageChanged(string newLang)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Language changed to: {newLang}");
        UpdateBioText();
    }

    private void UpdateBioText()
    {
        if (bioText != null && !string.IsNullOrEmpty(cachedBio))
            bioText.text = GetBioPrefix() + cachedBio;
    }

    public void ResetCurrentProfile()
    {
        currentProfileUid = "NOT_SET";
        _hasPendingFriend = false;
        _pendingFriendUid = null;
        if (debugLogs) Debug.Log("[ProfileManager] ResetCurrentProfile called");
    }

    public void ForceLoadProfile(string uid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] ForceLoadProfile uid={uid ?? "null (my profile)"}");
        currentProfileUid = "NOT_SET";
        _hasPendingFriend = false;
        _pendingFriendUid = null;
        LoadProfile(uid);
    }

    public void LoadProfile(string uid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] LoadProfile uid={uid ?? "null"}, activeInHierarchy={gameObject.activeInHierarchy}");

        if (!gameObject.activeInHierarchy)
        {
            _pendingFriendUid = uid;
            _hasPendingFriend = uid != null;
            if (debugLogs) Debug.Log($"[ProfileManager] Object inactive, queued uid={uid}");
            return;
        }

        if (uid == currentProfileUid)
        {
            if (debugLogs) Debug.Log($"[ProfileManager] Profile '{uid}' already loaded, skipping");
            return;
        }

        currentProfileUid = uid;

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
            StartCoroutine(WaitForSessionAndLoad());
            return;
        }

        SetTabsContainerVisible(true);
        SetBackButtonVisible(false);

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

        if (friendsPanelBinder != null && TokenManager.Instance.cachedFriends != null)
        {
            string myUid = profileResponse.data.uid;
            friendsPanelBinder.Apply(TokenManager.Instance.cachedFriends, myUid);
            if (debugLogs) Debug.Log("[ProfileManager] Friends panel applied for my profile");
        }
    }

    private IEnumerator WaitForSessionAndLoad()
    {
        while (TokenManager.Instance != null && !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        LoadMyProfile();
    }

    // ========== ПРОФИЛЬ ДРУГА / НАЙДЕННОГО ПОЛЬЗОВАТЕЛЯ ==========

    private IEnumerator LoadFriendProfileCoroutine(string friendUid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Loading friend profile: {friendUid}");

        while (TokenManager.Instance != null && !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        if (TokenManager.Instance == null)
        {
            Debug.LogError("[ProfileManager] TokenManager.Instance is null!");
            yield break;
        }

        SetTabsContainerVisible(false);
        SetBackButtonVisible(true);

        // 1. Ищем в кэше друзей
        TokenManager.FriendData friendData = FindFriendInCache(friendUid);

        if (friendData != null)
        {
            if (debugLogs) Debug.Log($"[ProfileManager] Found {friendUid} in cache");
            UpdateUI(new TokenManager.UserProfileData
            {
                uid           = friendData.uid,
                displayName   = friendData.displayName,
                discriminator = friendData.discriminator,
                bio           = friendData.bio,
                photoURL      = friendData.photoURL,
                stats         = new TokenManager.UserProfileStats { level = friendData.level }
            }, null);
        }
        else
        {
            // 2. Не в кэше — пользователь найден через поиск, тянем через /friends/user/:uid
            if (debugLogs) Debug.Log($"[ProfileManager] {friendUid} not in cache, fetching via GetFriendsByUid");

            TokenManager.FriendsResponse searchResult = null;
            yield return TokenManager.Instance.GetFriendsByUid(friendUid, r => searchResult = r);

            var found = searchResult?.data != null && searchResult.data.Length > 0
                ? searchResult.data[0] : null;

            if (found != null)
            {
                UpdateUI(new TokenManager.UserProfileData
                {
                    uid           = found.uid,
                    displayName   = found.displayName,
                    discriminator = found.discriminator,
                    bio           = found.bio,
                    photoURL      = found.photoURL,
                    stats         = new TokenManager.UserProfileStats { level = found.level }
                }, null);
            }
            else
            {
                ShowFallbackProfile(friendUid);
            }
        }

        // 3. Пинованные ачивки
        if (pinnedBinder != null && TokenManager.Instance.achievementsAll != null)
        {
            TokenManager.UserAchievementListResponse friendAchievements = null;
            yield return TokenManager.Instance.GetAchievementsByUid(friendUid, r => friendAchievements = r);

            if (friendAchievements != null)
            {
                pinnedBinder.Apply(TokenManager.Instance.achievementsAll, friendAchievements);
                if (debugLogs) Debug.Log($"[ProfileManager] Pinned achievements applied for {friendUid}");
            }
            else
            {
                Debug.LogWarning($"[ProfileManager] Could not load achievements for {friendUid}");
            }
        }

        // 4. Друзья найденного пользователя
        if (friendsPanelBinder != null)
        {
            TokenManager.FriendsResponse friendsFriends = null;
            yield return TokenManager.Instance.GetFriendsByUid(friendUid, r => friendsFriends = r);

            if (friendsFriends != null)
            {
                friendsPanelBinder.Apply(friendsFriends, friendUid);
                if (debugLogs) Debug.Log($"[ProfileManager] Friends panel applied for {friendUid}, count={friendsFriends.data?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning($"[ProfileManager] Could not load friends for {friendUid}");
            }
        }
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
        if (nameText    != null) nameText.text      = "Unknown #0000";
        if (levelText   != null) levelText.text     = "lvl ?";
        if (bioText     != null) bioText.text       = "";
        if (avatarImage != null) avatarImage.sprite = null;
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

        cachedBio = !string.IsNullOrEmpty(profile.bio) ? profile.bio : "...";
        UpdateBioText();

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

    // ========== ХЕЛПЕРЫ ==========

    private void SetTabsContainerVisible(bool visible)
    {
        if (tabsContainer == null) return;
        tabsContainer.SetActive(visible);
        if (debugLogs) Debug.Log($"[ProfileManager] TabsContainer.SetActive({visible})");
    }

    private void SetBackButtonVisible(bool visible)
    {
        if (backButton == null) return;
        backButton.SetActive(visible);
        if (debugLogs) Debug.Log($"[ProfileManager] BackButton.SetActive({visible})");
    }

    public void OnBackButtonClicked()
    {
        if (debugLogs) Debug.Log("[ProfileManager] BackButton clicked, returning to my profile");

        if (boardSlideSwitcher != null)
            boardSlideSwitcher.ForceOpenMyProfile();
        else
            ForceLoadProfile(null);
    }
}
