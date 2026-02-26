using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FriendTab
{
    AllFriends,
    PendingSent,
    PendingReceived
}

public class FriendsTabController : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button tabFriendsButton;
    [SerializeField] private Button tabPendingSentButton;
    [SerializeField] private Button tabPendingReceivedButton;

    [Header("Tab Borders (optional)")]
    [SerializeField] private Image tabFriendsBorder;
    [SerializeField] private Image tabPendingSentBorder;
    [SerializeField] private Image tabPendingReceivedBorder;

    [Header("Tab Texts")]
    [SerializeField] private TextMeshProUGUI tabFriendsText;
    [SerializeField] private TextMeshProUGUI tabPendingSentText;
    [SerializeField] private TextMeshProUGUI tabPendingReceivedText;

    [Header("Colors")]
    [SerializeField] private Color activeBorderColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color inactiveBorderColor = Color.clear;
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(0.6f, 0.6f, 0.6f);

    private FriendTab currentTab = FriendTab.AllFriends;

    public System.Action<FriendTab> OnTabChanged;

    private void Start()
    {
        tabFriendsButton.onClick.AddListener(() => SwitchTab(FriendTab.AllFriends));
        tabPendingSentButton.onClick.AddListener(() => SwitchTab(FriendTab.PendingSent));
        tabPendingReceivedButton.onClick.AddListener(() => SwitchTab(FriendTab.PendingReceived));

        // Устанавливаем визуал первой вкладки без вызова события
        UpdateTabVisuals(FriendTab.AllFriends, tabFriendsBorder, tabFriendsText);
        UpdateTabVisuals(FriendTab.PendingSent, tabPendingSentBorder, tabPendingSentText);
        UpdateTabVisuals(FriendTab.PendingReceived, tabPendingReceivedBorder, tabPendingReceivedText);
    }

    public void SwitchTab(FriendTab newTab)
    {
        currentTab = newTab;

        UpdateTabVisuals(FriendTab.AllFriends, tabFriendsBorder, tabFriendsText);
        UpdateTabVisuals(FriendTab.PendingSent, tabPendingSentBorder, tabPendingSentText);
        UpdateTabVisuals(FriendTab.PendingReceived, tabPendingReceivedBorder, tabPendingReceivedText);

        OnTabChanged?.Invoke(currentTab);

        Debug.Log($"[FriendsTabController] Switched to tab: {currentTab}");
    }

    private void UpdateTabVisuals(FriendTab tab, Image border, TextMeshProUGUI text)
    {
        bool isActive = (currentTab == tab);

        if (border != null)
        {
            border.color = isActive ? activeBorderColor : inactiveBorderColor;
        }

        if (text != null)
        {
            text.color = isActive ? activeTextColor : inactiveTextColor;
            text.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            text.fontSize = isActive ? 16 : 14;
        }
    }

    public void UpdateTabCounts(int friendsCount, int sentCount, int receivedCount)
    {
        if (tabFriendsText != null)
            tabFriendsText.text = $"Друзья ({friendsCount})";
        
        if (tabPendingSentText != null)
            tabPendingSentText.text = $"Отправлено ({sentCount})";
        
        if (tabPendingReceivedText != null)
            tabPendingReceivedText.text = $"Входящие ({receivedCount})";
    }

    public FriendTab GetCurrentTab()
    {
        return currentTab;
    }
}
