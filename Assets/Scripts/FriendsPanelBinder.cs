using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FriendsPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private FriendItemView itemPrefab;

    [Header("Root")]
    [SerializeField] private Transform verticalContent;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Tab Controller")]
    [SerializeField] private FriendsTabController tabController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private TokenManager.FriendData[] allFriendsData;
    private string currentUserUid;

    private void OnEnable()
    {
        if (tabController != null)
        {
            tabController.OnTabChanged += OnTabChanged;
            Debug.Log("[FriendsPanelBinder] OnEnable - subscribed to OnTabChanged");
        }
    }

    private void OnDisable()
    {
        if (tabController != null)
        {
            tabController.OnTabChanged -= OnTabChanged;
            Debug.Log("[FriendsPanelBinder] OnDisable - unsubscribed from OnTabChanged");
        }
    }

    /// <summary>
    /// Применяет данные друзей к панели
    /// </summary>
    public void Apply(TokenManager.FriendsResponse friendsResp, string myUid)
    {
        Debug.Log($"[FriendsPanelBinder] Apply called with {friendsResp?.data?.Length ?? -1} friends");
        
        if (!verticalContent || !itemPrefab)
        {
            Debug.LogError("[FriendsPanelBinder] References not set.");
            return;
        }

        currentUserUid = myUid;

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Apply START - myUid: '{myUid}'");

        if (friendsResp?.data == null || friendsResp.data.Length == 0)
        {
            allFriendsData = new TokenManager.FriendData[0];
            
            if (debugLogs)
                Debug.Log("[FriendsPanelBinder] No friends data.");
            
            UpdateTabCounts();
            ClearContent();
            return;
        }

        allFriendsData = friendsResp.data;

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Loaded {allFriendsData.Length} friends total");

        UpdateTabCounts();
        
        // Отображаем текущую вкладку
        OnTabChanged(tabController?.GetCurrentTab() ?? FriendTab.AllFriends);
    }

    /// <summary>
    /// Обновляет счетчики на вкладках
    /// </summary>
    private void UpdateTabCounts()
    {
        if (tabController == null || allFriendsData == null) return;

        int friendsCount = 0;
        int sentCount = 0;
        int receivedCount = 0;

        foreach (var friend in allFriendsData)
        {
            switch (friend.status)
            {
                case "active":
                    friendsCount++;
                    break;
                case "pending_sent":
                    sentCount++;
                    break;
                case "pending_received":
                    receivedCount++;
                    break;
            }
        }

        tabController.UpdateTabCounts(friendsCount, sentCount, receivedCount);

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Tab counts updated: friends={friendsCount}, sent={sentCount}, received={receivedCount}");
    }

    /// <summary>
    /// Вызывается при переключении вкладки
    /// </summary>
    private void OnTabChanged(FriendTab tab)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Tab changed to: {tab}, allFriendsData={(allFriendsData != null ? allFriendsData.Length.ToString() : "NULL")}");

        ClearContent();

        if (allFriendsData == null)
        {
            if (debugLogs)
                Debug.LogWarning("[FriendsPanelBinder] allFriendsData is NULL!");
            return;
        }

        if (allFriendsData.Length == 0)
        {
            if (debugLogs)
                Debug.Log("[FriendsPanelBinder] No friends to display (length=0)");
            return;
        }

        // Фильтруем по статусу
        string statusFilter = GetStatusForTab(tab);
        int displayedCount = 0;

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Filtering by status: {statusFilter}");

        foreach (var friend in allFriendsData)
        {
            if (debugLogs)
                Debug.Log($"[FriendsPanelBinder] Checking friend {friend.displayName}, status={friend.status}, matches={friend.status == statusFilter}");

            if (friend.status == statusFilter)
            {
                CreateFriendItem(friend);
                displayedCount++;
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Displayed {displayedCount} friends for tab {tab}, total children: {verticalContent.childCount}");
    }

    /// <summary>
    /// Возвращает статус друга для вкладки
    /// </summary>
    private string GetStatusForTab(FriendTab tab)
    {
        switch (tab)
        {
            case FriendTab.AllFriends:
                return "active";
            case FriendTab.PendingSent:
                return "pending_sent";
            case FriendTab.PendingReceived:
                return "pending_received";
            default:
                return "active";
        }
    }

    /// <summary>
    /// Создает элемент друга
    /// </summary>
    private void CreateFriendItem(TokenManager.FriendData friend)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("[FriendsPanelBinder] itemPrefab is NULL!");
            return;
        }

        var view = Instantiate(itemPrefab, verticalContent, false);
        
        if (view == null)
        {
            Debug.LogError("[FriendsPanelBinder] Failed to instantiate item!");
            return;
        }
        
        view.gameObject.SetActive(true);

        NormalizeRect(view.transform);

        view.Bind(
            uid: friend.uid,
            displayName: friend.displayName ?? "Unknown",
            discriminator: friend.discriminator ?? "0000",
            level: friend.level,
            photoURL: friend.photoURL,
            status: friend.status
        );

        // Подписываемся на события кнопок
        view.OnProfileClicked += OpenFriendProfile;
        view.OnRemoveClicked += RemoveFriend;
        view.OnAcceptClicked += AcceptFriendRequest;

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Created item for {friend.displayName}#{friend.discriminator}, status={friend.status}");
    }

    /// <summary>
    /// Очищает контент
    /// </summary>
    private void ClearContent()
    {
        if (verticalContent == null) return;

        int childCount = verticalContent.childCount;
        
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Clearing content, children before: {childCount}");

        for (int i = verticalContent.childCount - 1; i >= 0; i--)
        {
            var child = verticalContent.GetChild(i).gameObject;
            
            // В редакторе удаляем сразу, в билде - обычный Destroy
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
            #else
            Destroy(child);
            #endif
        }
        
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Content cleared, children after: {verticalContent.childCount}");
    }

    /// <summary>
    /// Нормализует RectTransform
    /// </summary>
    private static void NormalizeRect(Transform t)
    {
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;

        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
        else
            t.localPosition = Vector3.zero;
    }

    // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

    private void OpenFriendProfile(string uid)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Opening profile for: {uid}");

        // TODO: Открыть профиль друга
        ProfileManager profileManager = FindFirstObjectByType<ProfileManager>();
        if (profileManager != null)
        {
            profileManager.LoadProfile(uid);
        }
        else
        {
            Debug.LogWarning("[FriendsPanelBinder] ProfileManager not found!");
        }
    }

    private void RemoveFriend(string friendId)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Removing friend: {friendId}");

        StartCoroutine(TokenManager.Instance.RemoveFriend(friendId, OnFriendRemoved));
    }

    private void OnFriendRemoved(bool success, string error)
    {
        if (success)
        {
            if (debugLogs)
                Debug.Log("[FriendsPanelBinder] Friend removed successfully");

            // Обновляем список
            TokenManager.Instance.RefreshFriends();
        }
        else
        {
            Debug.LogError($"[FriendsPanelBinder] Error removing friend: {error}");
        }
    }

    private void AcceptFriendRequest(string friendId)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Accepting friend request from: {friendId}");

        StartCoroutine(TokenManager.Instance.AcceptFriendRequest(friendId, OnFriendRequestAccepted));
    }

    private void OnFriendRequestAccepted(bool success, string error)
    {
        if (success)
        {
            if (debugLogs)
                Debug.Log("[FriendsPanelBinder] Friend request accepted successfully");

            // Обновляем список
            TokenManager.Instance.RefreshFriends();
        }
        else
        {
            Debug.LogError($"[FriendsPanelBinder] Error accepting friend request: {error}");
        }
    }

    // ========== ТЕСТОВЫЙ МЕТОД ==========
#if UNITY_EDITOR
    [ContextMenu("Test Fill Friends")]
    private void TestFillFriends()
    {
        Debug.Log("[FriendsPanelBinder] ========== TEST FILL FRIENDS START ==========");
        
        var testData = new TokenManager.FriendsResponse
        {
            success = true,
            data = new TokenManager.FriendData[]
            {
                // ===== АКТИВНЫЕ ДРУЗЬЯ (4 шт) =====
                new TokenManager.FriendData
                {
                    uid = "friend1",
                    displayName = "CodeMaster",
                    discriminator = "1234",
                    level = 15,
                    status = "active",
                    photoURL = "",
                    bio = "Java expert"
                },
                new TokenManager.FriendData
                {
                    uid = "friend2",
                    displayName = "DragonSlayer",
                    discriminator = "5678",
                    level = 22,
                    status = "active",
                    photoURL = "",
                    bio = "Pro gamer"
                },
                new TokenManager.FriendData
                {
                    uid = "friend3",
                    displayName = "JavaGuru",
                    discriminator = "9999",
                    level = 18,
                    status = "active",
                    photoURL = "",
                    bio = "Loves coffee"
                },
                new TokenManager.FriendData
                {
                    uid = "friend4",
                    displayName = "SyntaxNinja",
                    discriminator = "4444",
                    level = 12,
                    status = "active",
                    photoURL = "",
                    bio = "Code fast"
                },
                
                // ===== ОТПРАВЛЕННЫЕ ЗАПРОСЫ (2 шт) =====
                new TokenManager.FriendData
                {
                    uid = "pending1",
                    displayName = "NewPlayer",
                    discriminator = "1111",
                    level = 5,
                    status = "pending_sent",
                    photoURL = "",
                    bio = "Just started"
                },
                new TokenManager.FriendData
                {
                    uid = "pending2",
                    displayName = "LoopMaster",
                    discriminator = "2222",
                    level = 10,
                    status = "pending_sent",
                    photoURL = "",
                    bio = "While loops expert"
                },
                
                // ===== ВХОДЯЩИЕ ЗАПРОСЫ (3 шт) =====
                new TokenManager.FriendData
                {
                    uid = "incoming1",
                    displayName = "BugHunter",
                    discriminator = "3333",
                    level = 12,
                    status = "pending_received",
                    photoURL = "",
                    bio = "Found all bugs"
                },
                new TokenManager.FriendData
                {
                    uid = "incoming2",
                    displayName = "PixelWarrior",
                    discriminator = "5555",
                    level = 8,
                    status = "pending_received",
                    photoURL = "",
                    bio = "Graphics lover"
                },
                new TokenManager.FriendData
                {
                    uid = "incoming3",
                    displayName = "AlgoMaster",
                    discriminator = "6666",
                    level = 20,
                    status = "pending_received",
                    photoURL = "",
                    bio = "O(1) everything"
                }
            }
        };

        Apply(testData, "current_user");

        Debug.Log("[FriendsPanelBinder] ========== TEST FILL FRIENDS END ==========");
        Debug.Log("[FriendsPanelBinder] Test data filled: 4 friends, 2 sent, 3 received!");
    }
#endif
}
