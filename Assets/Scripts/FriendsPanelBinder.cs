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

    [Header("Board Switcher (profile panel animation)")]
    [SerializeField] private BoardSlideSwitcher boardSlideSwitcher;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private TokenManager.FriendData[] allFriendsData;
    private string currentUserUid;
    private bool _isMyProfile = true;
    private bool _hasPendingData = false;

    private void OnEnable()
    {
        if (tabController != null)
            tabController.OnTabChanged += OnTabChanged;

        if (_hasPendingData)
            RebuildUI();
    }

    private void OnDisable()
    {
        if (tabController != null)
            tabController.OnTabChanged -= OnTabChanged;
    }

    public void Apply(TokenManager.FriendsResponse friendsResp, string myUid)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Apply called with {friendsResp?.data?.Length ?? -1} friends");

        string selfUid = TokenManager.Instance?.profile?.data?.uid;
        _isMyProfile = (myUid == selfUid);
        currentUserUid = myUid;
        allFriendsData = friendsResp?.data ?? new TokenManager.FriendData[0];
        _hasPendingData = true;

        if (!gameObject.activeInHierarchy) return;

        RebuildUI();
    }

    private void RebuildUI()
    {
        _hasPendingData = false;

        if (!verticalContent || !itemPrefab)
        {
            Debug.LogError("[FriendsPanelBinder] References not set.");
            return;
        }

        if (tabController != null)
        {
            tabController.OnTabChanged -= OnTabChanged;
            tabController.OnTabChanged += OnTabChanged;
        }

        UpdateTabCounts();
        tabController?.SwitchTab(FriendTab.AllFriends);
    }

    private void UpdateTabCounts()
    {
        if (tabController == null || allFriendsData == null) return;

        int friendsCount  = 0;
        int sentCount     = 0;
        int receivedCount = 0;

        foreach (var friend in allFriendsData)
        {
            switch (friend.status)
            {
                case "active":           friendsCount++;  break;
                case "pending_sent":     sentCount++;     break;
                case "pending_received": receivedCount++; break;
            }
        }

        tabController.UpdateTabCounts(friendsCount, sentCount, receivedCount);
    }

    private void OnTabChanged(FriendTab tab)
    {
        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Tab changed to: {tab}");

        ClearContent();

        if (allFriendsData == null || allFriendsData.Length == 0) return;

        string statusFilter   = GetStatusForTab(tab);
        int    displayedCount = 0;

        foreach (var friend in allFriendsData)
        {
            if (friend.status == statusFilter)
            {
                CreateFriendItem(friend);
                displayedCount++;
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);

        if (debugLogs)
            Debug.Log($"[FriendsPanelBinder] Displayed {displayedCount} friends for tab {tab}");
    }

    private string GetStatusForTab(FriendTab tab)
    {
        return tab switch
        {
            FriendTab.AllFriends      => "active",
            FriendTab.PendingSent     => "pending_sent",
            FriendTab.PendingReceived => "pending_received",
            _                         => "active"
        };
    }

    private void CreateFriendItem(TokenManager.FriendData friend)
    {
        if (itemPrefab == null) return;

        var view = Instantiate(itemPrefab, verticalContent, false);
        if (view == null) return;

        view.gameObject.SetActive(true);
        NormalizeRect(view.transform);

        view.Bind(
            uid:           friend.uid,
            displayName:   friend.displayName   ?? "Unknown",
            discriminator: friend.discriminator ?? "0000",
            level:         friend.level,
            photoURL:      friend.photoURL,
            status:        friend.status,
            isMyProfile:   _isMyProfile
        );

        view.OnProfileClicked += OpenFriendProfile;
        view.OnRemoveClicked  += RemoveFriend;
        view.OnAcceptClicked  += AcceptFriendRequest;
        view.OnDeclineClicked += DeclineFriendRequest;
    }

    private void ClearContent()
    {
        if (verticalContent == null) return;

        for (int i = verticalContent.childCount - 1; i >= 0; i--)
        {
            var child = verticalContent.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private static void NormalizeRect(Transform t)
    {
        t.localScale    = Vector3.one;
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
            Debug.Log($"[FriendsPanelBinder] OpenFriendProfile uid={uid}");

        if (boardSlideSwitcher != null)
            boardSlideSwitcher.ForceOpenProfile(uid);
        else
            Debug.LogWarning("[FriendsPanelBinder] BoardSlideSwitcher not set!");
    }

    private void RemoveFriend(string friendId)
    {
        StartCoroutine(TokenManager.Instance.RemoveFriend(friendId, OnFriendRemoved));
    }

    private void OnFriendRemoved(bool success, string error)
    {
        if (success) TokenManager.Instance.RefreshFriends();
        else Debug.LogError($"[FriendsPanelBinder] Error removing friend: {error}");
    }

    private void AcceptFriendRequest(string friendId)
    {
        StartCoroutine(TokenManager.Instance.AcceptFriendRequest(friendId, OnFriendRequestAccepted));
    }

    private void OnFriendRequestAccepted(bool success, string error)
    {
        if (success) TokenManager.Instance.RefreshFriends();
        else Debug.LogError($"[FriendsPanelBinder] Error accepting friend request: {error}");
    }

    private void DeclineFriendRequest(string friendId)
    {
        StartCoroutine(TokenManager.Instance.DeclineFriendRequest(friendId, OnFriendRequestDeclined));
    }

    private void OnFriendRequestDeclined(bool success)
    {
        if (success) TokenManager.Instance.RefreshFriends();
        else Debug.LogError("[FriendsPanelBinder] Error declining friend request");
    }

#if UNITY_EDITOR
    [ContextMenu("Test Fill Friends")]
    private void TestFillFriends()
    {
        var testData = new TokenManager.FriendsResponse
        {
            success = true,
            data = new TokenManager.FriendData[]
            {
                new TokenManager.FriendData { uid="friend1",   displayName="CodeMaster",  discriminator="1234", level=15, status="active",           photoURL="", bio="Java expert"     },
                new TokenManager.FriendData { uid="friend2",   displayName="DragonSlayer", discriminator="5678", level=22, status="active",           photoURL="", bio="Pro gamer"       },
                new TokenManager.FriendData { uid="friend3",   displayName="JavaGuru",     discriminator="9999", level=18, status="active",           photoURL="", bio="Loves coffee"    },
                new TokenManager.FriendData { uid="pending1",  displayName="NewPlayer",    discriminator="1111", level=5,  status="pending_sent",     photoURL="", bio="Just started"    },
                new TokenManager.FriendData { uid="incoming1", displayName="BugHunter",    discriminator="3333", level=12, status="pending_received", photoURL="", bio="Found all bugs"  },
                new TokenManager.FriendData { uid="incoming2", displayName="AlgoMaster",   discriminator="6666", level=20, status="pending_received", photoURL="", bio="O(1) everything" },
            }
        };
        Apply(testData, "current_user");
    }
#endif
}
