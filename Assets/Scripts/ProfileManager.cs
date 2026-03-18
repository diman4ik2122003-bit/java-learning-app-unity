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

    [Header("Friend Action Buttons")]
    [SerializeField] private GameObject addFriendButton;
    [SerializeField] private GameObject acceptFriendButton;
    [SerializeField] private GameObject declineFriendButton;
    [SerializeField] private GameObject removeFriendButton;

    [Header("Navigation")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private BoardSlideSwitcher boardSlideSwitcher;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string currentProfileUid = "NOT_SET";
    private string cachedBio;

    private string _pendingFriendUid   = null;
    private bool   _hasPendingFriend   = false;
    private TokenManager.FriendData _pendingFriendData = null;

    // ========== ЛОКАЛИЗАЦИЯ ==========

    private string GetBioPrefix()
    {
        string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
        return lang == "ru" ? "О себе: " : "Bio: ";
    }

    // ========== LIFECYCLE ==========

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

        _hasPendingFriend  = false;
        _pendingFriendUid  = null;
        _pendingFriendData = null;
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

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void ResetCurrentProfile()
    {
        currentProfileUid  = "NOT_SET";
        _hasPendingFriend  = false;
        _pendingFriendUid  = null;
        _pendingFriendData = null;
        if (debugLogs) Debug.Log("[ProfileManager] ResetCurrentProfile called");
    }

    public void ForceLoadProfile(string uid)
    {
        if (debugLogs) Debug.Log($"[ProfileManager] ForceLoadProfile uid={uid ?? "null (my profile)"}");
        currentProfileUid  = "NOT_SET";
        _hasPendingFriend  = false;
        _pendingFriendUid  = null;
        _pendingFriendData = null;
        LoadProfile(uid);
    }

    // ↓ НОВЫЙ МЕТОД — вызывается из UserSearchPanel перед открытием панели
    public void SetPendingData(TokenManager.FriendData data)
    {
        _pendingFriendData = data;
        if (debugLogs) Debug.Log($"[ProfileManager] SetPendingData uid={data?.uid}");
    }

    public void LoadFriendProfile(TokenManager.FriendData data)
    {
        if (data == null) return;
        if (debugLogs) Debug.Log($"[ProfileManager] LoadFriendProfile uid={data.uid}");

        _pendingFriendData = data;

        if (!gameObject.activeInHierarchy)
        {
            _pendingFriendUid = data.uid;
            _hasPendingFriend = true;
            if (debugLogs) Debug.Log($"[ProfileManager] Object inactive, queued uid={data.uid}");
            return;
        }

        currentProfileUid = "NOT_SET";
        LoadProfile(data.uid);
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
        HideAllFriendButtons();

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
            if (debugLogs) Debug.Log($"[ProfileManager] Found {friendUid} in cache, status={friendData.status}");
            UpdateUI(new TokenManager.UserProfileData
            {
                uid           = friendData.uid,
                displayName   = friendData.displayName,
                discriminator = friendData.discriminator,
                bio           = friendData.bio,
                photoURL      = friendData.photoURL,
                stats         = new TokenManager.UserProfileStats { level = friendData.level }
            }, null);
            ApplyFriendButtons(friendData.status);
            _pendingFriendData = null;
        }
        // 2. Есть данные из поиска — используем напрямую
        else if (_pendingFriendData != null && _pendingFriendData.uid == friendUid)
        {
            if (debugLogs) Debug.Log($"[ProfileManager] Using search data for {friendUid}, status={_pendingFriendData.status}");
            UpdateUI(new TokenManager.UserProfileData
            {
                uid           = _pendingFriendData.uid,
                displayName   = _pendingFriendData.displayName,
                discriminator = _pendingFriendData.discriminator,
                bio           = _pendingFriendData.bio,
                photoURL      = _pendingFriendData.photoURL,
                stats         = new TokenManager.UserProfileStats { level = _pendingFriendData.level }
            }, null);
            ApplyFriendButtons(_pendingFriendData.status);
            _pendingFriendData = null;
        }
        else
        {
            // 3. Нет данных нигде — fallback
            if (debugLogs) Debug.Log($"[ProfileManager] No data for {friendUid}, showing fallback");
            ShowFallbackProfile(friendUid);
            ApplyFriendButtons(null);
            _pendingFriendData = null;
        }

        // 4. Пинованные ачивки
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
                Debug.LogWarning($"[ProfileManager] Could not load achievements for {friendUid}");
        }

        // 5. Друзья найденного пользователя
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
                Debug.LogWarning($"[ProfileManager] Could not load friends for {friendUid}");
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

    // ========== КНОПКИ ДРУЖБЫ ==========

    private void ApplyFriendButtons(string status)
    {
        HideAllFriendButtons();
        if (debugLogs) Debug.Log($"[ProfileManager] ApplyFriendButtons status={status ?? "null"}");

        switch (status)
        {
            case "active":
                if (removeFriendButton  != null) removeFriendButton.SetActive(true);
                break;

            case "pending_sent":
                if (addFriendButton     != null) addFriendButton.SetActive(true);
                break;

            case "pending_received":
                if (acceptFriendButton  != null) acceptFriendButton.SetActive(true);
                if (declineFriendButton != null) declineFriendButton.SetActive(true);
                break;

            default:
                if (addFriendButton     != null) addFriendButton.SetActive(true);
                break;
        }
    }

    private void HideAllFriendButtons()
    {
        if (addFriendButton     != null) addFriendButton.SetActive(false);
        if (acceptFriendButton  != null) acceptFriendButton.SetActive(false);
        if (declineFriendButton != null) declineFriendButton.SetActive(false);
        if (removeFriendButton  != null) removeFriendButton.SetActive(false);
    }

    // ========== ОБРАБОТЧИКИ КНОПОК ==========

    public void OnAddFriendClicked()
    {
        string status = GetCachedStatus(currentProfileUid);

        if (status == "pending_sent")
        {
            PopupManager.Instance?.Show("Запрос уже отправлен");
            return;
        }

        if (debugLogs) Debug.Log($"[ProfileManager] Sending friend request to {currentProfileUid}");
        StartCoroutine(SendFriendRequestCoroutine(currentProfileUid));
    }

    public void OnAcceptFriendClicked()
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Accepting friend request from {currentProfileUid}");
        StartCoroutine(AcceptFriendCoroutine(currentProfileUid));
    }

    public void OnDeclineFriendClicked()
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Declining friend request from {currentProfileUid}");
        StartCoroutine(DeclineFriendCoroutine(currentProfileUid));
    }

    public void OnRemoveFriendClicked()
    {
        if (debugLogs) Debug.Log($"[ProfileManager] Removing friend {currentProfileUid}");
        StartCoroutine(RemoveFriendCoroutine(currentProfileUid));
    }

    private string GetCachedStatus(string uid)
    {
        var data = TokenManager.Instance?.cachedFriends?.data;
        if (data == null) return null;
        foreach (var f in data)
            if (f.uid == uid) return f.status;
        return null;
    }

    private IEnumerator SendFriendRequestCoroutine(string uid)
    {
        bool success = false;
        yield return TokenManager.Instance.SendFriendRequest(uid, r => success = r);

        if (success)
        {
            PopupManager.Instance?.Show("Запрос отправлен!");
            ApplyFriendButtons("pending_sent");
        }
        else
            PopupManager.Instance?.Show("Не удалось отправить запрос");
    }

    private IEnumerator AcceptFriendCoroutine(string uid)
    {
        bool success = false;
        yield return TokenManager.Instance.AcceptFriendRequest(uid, r => success = r);

        if (success)
        {
            PopupManager.Instance?.Show("Теперь вы друзья!");
            ApplyFriendButtons("active");
        }
        else
            PopupManager.Instance?.Show("Не удалось принять запрос");
    }

    private IEnumerator DeclineFriendCoroutine(string uid)
    {
        bool success = false;
        yield return TokenManager.Instance.DeclineFriendRequest(uid, r => success = r);

        if (success)
        {
            PopupManager.Instance?.Show("Запрос отклонён");
            ApplyFriendButtons(null);
        }
        else
            PopupManager.Instance?.Show("Не удалось отклонить запрос");
    }

    private IEnumerator RemoveFriendCoroutine(string uid)
    {
        var cached = FindFriendInCache(uid); // ← сохраняем ДО запроса

    bool success = false;
    yield return TokenManager.Instance.RemoveFriend(uid, r => success = r);

    if (success)
    {
        PopupManager.Instance?.Show("Пользователь удалён из друзей");
        ApplyFriendButtons(null);

        if (cached != null)
            _pendingFriendData = cached; // ← теперь повторный просмотр профиля работает
    }
    else
        PopupManager.Instance?.Show("Не удалось удалить из друзей");
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
