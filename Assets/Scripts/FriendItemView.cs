using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class FriendItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private Image avatar;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Menu Button (три точки)")]
    [SerializeField] private Button menuButton;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private Button viewProfileButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = new Color(0.17f, 0.17f, 0.17f);

    private string friendUid;
    private string friendStatus;
    private string _lastLoadedUrl;
    private RectTransform contextMenuRect;
    private Transform originalParent;

    public event Action<string> OnProfileClicked;
    public event Action<string> OnRemoveClicked;
    public event Action<string> OnAcceptClicked;
    public event Action<string> OnDeclineClicked;

    private void Awake()
    {
        if (contextMenuPanel != null)
        {
            contextMenuRect = contextMenuPanel.GetComponent<RectTransform>();
            originalParent  = contextMenuPanel.transform.parent;
        }

        menuButton?.onClick.AddListener(ToggleContextMenu);

        viewProfileButton?.onClick.AddListener(() =>
        {
            HideContextMenu();
            OnProfileClicked?.Invoke(friendUid);
        });

        removeButton?.onClick.AddListener(() =>
        {
            HideContextMenu();
            OnRemoveClicked?.Invoke(friendUid);
        });

        acceptButton?.onClick.AddListener(() =>
        {
            HideContextMenu();
            OnAcceptClicked?.Invoke(friendUid);
        });

        declineButton?.onClick.AddListener(() =>
        {
            HideContextMenu();
            OnDeclineClicked?.Invoke(friendUid);
        });

        HideContextMenu();
    }

    public void Bind(string uid, string displayName, string discriminator,
                     int level, string photoURL, string status, bool isMyProfile = true)
    {
        friendUid    = uid;
        friendStatus = status;

        if (nameText   != null) nameText.text  = $"{displayName}#{discriminator}";
        if (levelText  != null) levelText.text = $"lvl {level}";
        if (background != null) background.color = normalBackgroundColor;

        if (menuButton != null)
            menuButton.gameObject.SetActive(true);

        if (viewProfileButton != null)
            viewProfileButton.gameObject.SetActive(true);

        if (removeButton != null)
            removeButton.gameObject.SetActive(isMyProfile && (status == "active" || status == "pending_sent"));

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(isMyProfile && status == "pending_received");

        if (declineButton != null)
            declineButton.gameObject.SetActive(isMyProfile && status == "pending_received");

        if (!string.IsNullOrEmpty(photoURL) && photoURL != _lastLoadedUrl)
        {
            _lastLoadedUrl = photoURL;
            StartCoroutine(LoadAvatar(photoURL));
        }
        else if (string.IsNullOrEmpty(photoURL))
        {
            SetDefaultAvatar();
        }

        HideContextMenu();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void ToggleContextMenu()
    {
        if (contextMenuPanel == null) return;

        bool willBeVisible = !contextMenuPanel.activeSelf;

        foreach (var other in FindObjectsByType<FriendItemView>(FindObjectsSortMode.None))
            other.HideContextMenu();

        if (!willBeVisible) return;

        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        contextMenuRect.SetParent(rootCanvas.transform, true);

        PositionMenuNearButton(rootCanvas);

        contextMenuPanel.SetActive(true);
    }

    private void PositionMenuNearButton(Canvas rootCanvas)
    {
        if (menuButton == null || contextMenuRect == null) return;

        RectTransform buttonRect = menuButton.GetComponent<RectTransform>();

        Vector3[] buttonCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonCorners);
        Vector3 topRightWorld = buttonCorners[2];

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.worldCamera,
            topRightWorld
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            screenPoint,
            rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        contextMenuRect.anchorMin        = new Vector2(0.5f, 0.5f);
        contextMenuRect.anchorMax        = new Vector2(0.5f, 0.5f);
        contextMenuRect.pivot            = new Vector2(0f, 1f);
        contextMenuRect.anchoredPosition = localPoint;
    }

    public void HideContextMenu()
    {
        if (contextMenuPanel == null) return;
        contextMenuPanel.SetActive(false);
    }

    private IEnumerator LoadAvatar(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (avatar != null && texture != null)
                    avatar.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
            }
            else
            {
                SetDefaultAvatar();
            }
        }
    }

    private void SetDefaultAvatar()
    {
        if (avatar != null) avatar.color = Color.gray;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        if (contextMenuPanel != null && originalParent != null)
            contextMenuPanel.transform.SetParent(originalParent, false);

        menuButton?.onClick.RemoveAllListeners();
        viewProfileButton?.onClick.RemoveAllListeners();
        removeButton?.onClick.RemoveAllListeners();
        acceptButton?.onClick.RemoveAllListeners();
        declineButton?.onClick.RemoveAllListeners();
    }
}
