using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = new Color(0.17f, 0.17f, 0.17f);

    private string friendUid;
    private string friendStatus;
    private RectTransform contextMenuRect;
    private Transform originalParent;

    public event Action<string> OnProfileClicked;
    public event Action<string> OnRemoveClicked;
    public event Action<string> OnAcceptClicked;

    private void Awake()
    {
        contextMenuRect = contextMenuPanel.GetComponent<RectTransform>();
        originalParent  = contextMenuPanel.transform.parent;

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

        HideContextMenu();
    }

    public void Bind(string uid, string displayName, string discriminator,
                     int level, string photoURL, string status)
    {
        friendUid    = uid;
        friendStatus = status;

        if (nameText   != null) nameText.text    = $"{displayName}#{discriminator}";
        if (levelText  != null) levelText.text   = $"lvl {level}";
        if (background != null) background.color = normalBackgroundColor;

        if (viewProfileButton != null)
            viewProfileButton.gameObject.SetActive(status == "active");

        if (removeButton != null)
            removeButton.gameObject.SetActive(status == "active" || status == "pending_sent");

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(status == "pending_received");

        if (!string.IsNullOrEmpty(photoURL))
            StartCoroutine(LoadAvatar(photoURL));
        else
            SetDefaultAvatar();

        HideContextMenu();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void ToggleContextMenu()
    {
        if (contextMenuPanel == null) return;

        bool willBeVisible = !contextMenuPanel.activeSelf;

        // Закрываем все другие открытые меню
        foreach (var other in FindObjectsByType<FriendItemView>(FindObjectsSortMode.None))
            other.HideContextMenu();

        if (!willBeVisible) return;

        // Переносим панель на уровень корневого Canvas — выходим из ScrollRect/Mask
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        contextMenuRect.SetParent(rootCanvas.transform, true);

        PositionMenuNearButton(rootCanvas);

        contextMenuPanel.SetActive(true);
        Debug.Log($"[FriendItemView] Context menu opened, uid={friendUid}");
    }

    private void PositionMenuNearButton(Canvas rootCanvas)
    {
        if (menuButton == null || contextMenuRect == null) return;

        RectTransform buttonRect = menuButton.GetComponent<RectTransform>();

        // Получаем 4 угла кнопки в мировых координатах
        // 0=bottomLeft, 1=topLeft, 2=topRight, 3=bottomRight
        Vector3[] buttonCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonCorners);
        Vector3 topRightWorld = buttonCorners[2];

        // Конвертируем правый верхний угол кнопки в экранные координаты
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.worldCamera,
            topRightWorld
        );

        // Конвертируем в локальные координаты Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            screenPoint,
            rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        // pivot (0, 1) = левый верхний угол меню встаёт в точку правого верхнего угла кнопки
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

    private void OnDestroy()
    {
        // Возвращаем панель обратно перед уничтожением
        if (contextMenuPanel != null && originalParent != null)
            contextMenuPanel.transform.SetParent(originalParent, false);

        menuButton?.onClick.RemoveAllListeners();
        viewProfileButton?.onClick.RemoveAllListeners();
        removeButton?.onClick.RemoveAllListeners();
        acceptButton?.onClick.RemoveAllListeners();
    }

    private IEnumerator LoadAvatar(string url)
    {
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (avatar != null)
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
}
