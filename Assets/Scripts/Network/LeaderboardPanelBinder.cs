using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private LeaderboardItemView itemPrefab;

    [Header("Root")]
    [SerializeField] private Transform verticalContent;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string currentUserUid;
    private RectTransform _pendingScrollTarget;

    private void OnEnable()
    {
        if (_pendingScrollTarget != null && scrollRect != null)
        {
            if (debugLogs)
                Debug.Log("[LeaderboardPanelBinder] OnEnable: executing deferred scroll");
            StartCoroutine(ScrollToItem(_pendingScrollTarget));
            _pendingScrollTarget = null;
        }
    }

    public void Apply(TokenManager.LeaderboardResponse leaderboardResp, string myUid)
    {
        if (!verticalContent || !itemPrefab)
        {
            Debug.LogError("[LeaderboardPanelBinder] References not set.");
            return;
        }

        currentUserUid = myUid;
        _pendingScrollTarget = null;

        if (debugLogs)
            Debug.Log($"[LeaderboardPanelBinder] Apply START - myUid: '{myUid}'");

        // Очистка старого контента
        for (int i = verticalContent.childCount - 1; i >= 0; i--)
            Destroy(verticalContent.GetChild(i).gameObject);

        var data = leaderboardResp?.data;
        if (data == null || data.leaderboard == null || data.leaderboard.Length == 0)
        {
            if (debugLogs)
                Debug.Log("[LeaderboardPanelBinder] No leaderboard data.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[LeaderboardPanelBinder] Loading {data.leaderboard.Length} entries");

        // Создаём элементы списка
        for (int i = 0; i < data.leaderboard.Length; i++)
        {
            var entry = data.leaderboard[i];
            var view = CreateItem(verticalContent);
            
            bool isMe = entry.uid == currentUserUid;
            
            if (debugLogs)
                Debug.Log($"[LeaderboardPanelBinder] Entry #{i+1}: uid='{entry.uid}' vs myUid='{currentUserUid}' => isMe={isMe}");
            
            view.Bind(
                rank: entry.rank,
                displayName: entry.displayName ?? "Player",
                discriminator: entry.discriminator ?? "0000",
                level: entry.level,
                xp: entry.xp,
                photoURL: entry.photoURL,
                isCurrentUser: isMe
            );

            if (debugLogs)
                Debug.Log($"[LeaderboardPanelBinder] Bind called for {entry.displayName}, photoURL='{entry.photoURL}', active={view.gameObject.activeSelf}");
            
            // Автопрокрутка к своей позиции
            if (isMe && scrollRect != null)
            {
                if (debugLogs)
                    Debug.Log($"[LeaderboardPanelBinder] Found current user at rank {entry.rank}, scheduling scroll");
                
                Canvas.ForceUpdateCanvases();

                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(ScrollToItem(view.transform as RectTransform));
                }
                else
                {
                    if (debugLogs)
                        Debug.Log("[LeaderboardPanelBinder] Deferring scroll (panel inactive)");
                    _pendingScrollTarget = view.transform as RectTransform;
                }
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);

        if (debugLogs)
            Debug.Log($"[LeaderboardPanelBinder] Build done. Items={verticalContent.childCount}");
    }

    private LeaderboardItemView CreateItem(Transform parent)
    {
        var view = Instantiate(itemPrefab, parent, false); 
        view.gameObject.SetActive(true); 
        
        NormalizeRect(view.transform);

        if (debugLogs) 
            LogRect("[LeaderboardPanelBinder] Item created", view.transform);

        return view;
    }

    private static void NormalizeRect(Transform t)
    {
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;

        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
        else
            t.localPosition = Vector3.zero;
    }

    private static void LogRect(string tag, Transform t)
    {
        if (t is not RectTransform rt) return;

        Debug.Log($"{tag} name={t.name} parent={t.parent?.name} " +
                  $"anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot} " +
                  $"anchoredPos={rt.anchoredPosition} sizeDelta={rt.sizeDelta}");
    }

    private IEnumerator ScrollToItem(RectTransform item)
    {
        yield return new WaitForEndOfFrame();
        
        if (scrollRect == null || item == null) 
        {
            if (debugLogs)
                Debug.LogWarning("[LeaderboardPanelBinder] ScrollToItem failed - scrollRect or item is null");
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        
        var contentPanel = scrollRect.content;
        var viewportHeight = scrollRect.viewport.rect.height;
        var contentHeight = contentPanel.rect.height;
        
        var itemPos = Mathf.Abs(item.anchoredPosition.y);
        var scrollPos = itemPos / (contentHeight - viewportHeight);
        
        scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(scrollPos);

        if (debugLogs)
            Debug.Log($"[LeaderboardPanelBinder] Scrolled to position: {scrollRect.verticalNormalizedPosition}");
    }

    // ========== ТЕСТОВЫЙ МЕТОД ==========
#if UNITY_EDITOR
    [ContextMenu("Test Fill Leaderboard")]
    private void TestFillLeaderboard()
    {
        var testData = new TokenManager.LeaderboardResponse
        {
            data = new TokenManager.LeaderboardData
            {
                leaderboard = new TokenManager.LeaderboardEntry[]
                {
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 1, 
                        uid = "user1", 
                        displayName = "DragonSlayer", 
                        discriminator = "1234", 
                        level = 25, 
                        xp = 15000, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 2, 
                        uid = "user2", 
                        displayName = "CodeMaster", 
                        discriminator = "5678", 
                        level = 22, 
                        xp = 12500, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 3, 
                        uid = "current_user", 
                        displayName = "YourNickname", 
                        discriminator = "9999", 
                        level = 18, 
                        xp = 9800, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 4, 
                        uid = "user4", 
                        displayName = "JavaGuru", 
                        discriminator = "1111", 
                        level = 15, 
                        xp = 7500, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 5, 
                        uid = "user5", 
                        displayName = "BugHunter", 
                        discriminator = "2222", 
                        level = 12, 
                        xp = 5800, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 6, 
                        uid = "user6", 
                        displayName = "LoopMaster", 
                        discriminator = "3333", 
                        level = 10, 
                        xp = 4200, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 7, 
                        uid = "user7", 
                        displayName = "PixelWarrior", 
                        discriminator = "4444", 
                        level = 9, 
                        xp = 3800, 
                        photoURL = "" 
                    },
                    new TokenManager.LeaderboardEntry 
                    { 
                        rank = 8, 
                        uid = "user8", 
                        displayName = "SyntaxNinja", 
                        discriminator = "5555", 
                        level = 8, 
                        xp = 3200, 
                        photoURL = "" 
                    },
                },
                total = 8,
                limit = 100,
                offset = 0
            }
        };
        
        Apply(testData, "current_user");
        
        Debug.Log("[LeaderboardPanelBinder] Test data filled with 8 players!");
    }
#endif
}
