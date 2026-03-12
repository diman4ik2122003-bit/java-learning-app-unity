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
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionButtonIcon;

    // НОВОЕ: кнопка на всю строку (Button на корневом FriendRowItem)
    [SerializeField] private Button rowButton;

    [Header("Button Icons")]
    [SerializeField] private Sprite profileIcon;
    [SerializeField] private Sprite cancelIcon;
    [SerializeField] private Sprite acceptIcon;

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = new Color(0.17f, 0.17f, 0.17f);
    [SerializeField] private Color profileIconColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color cancelIconColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private Color acceptIconColor = new Color(0.3f, 1f, 0.3f);

    private string friendUid;
    private string friendStatus;

    // Уже было:
    public event Action<string> OnProfileClicked;
    public event Action<string> OnRemoveClicked;
    public event Action<string> OnAcceptClicked;

    // НОВОЕ: событие клика по строке (отлично подойдет под "открыть профиль")
    public event Action<string> OnRowClicked;

    private void Awake()
    {
        Debug.Log($"[FriendItemView] Awake, rowButton={rowButton}, actionButton={actionButton}");

        if (actionButton != null)
            actionButton.onClick.AddListener(HandleButtonClick);

        // НОВОЕ: подписка на клик по всей строке
        if (rowButton != null)
            rowButton.onClick.AddListener(HandleRowClick);
    }

    public void Bind(string uid, string displayName, string discriminator, int level, string photoURL, string status)
    {
        friendUid = uid;
        friendStatus = status;

        if (nameText != null)
            nameText.text = $"{displayName}#{discriminator}";

        if (levelText != null)
            levelText.text = $"lvl {level}";

        SetupActionButton(status);

        if (!string.IsNullOrEmpty(photoURL))
            StartCoroutine(LoadAvatar(photoURL));
        else
            SetDefaultAvatar();

        if (background != null)
            background.color = normalBackgroundColor;

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void SetupActionButton(string status)
    {
        if (actionButtonIcon == null) return;

        switch (status)
        {
            case "active":
                if (profileIcon != null)
                    actionButtonIcon.sprite = profileIcon;
                actionButtonIcon.color = profileIconColor;
                break;

            case "pending_sent":
                if (cancelIcon != null)
                    actionButtonIcon.sprite = cancelIcon;
                actionButtonIcon.color = cancelIconColor;
                break;

            case "pending_received":
                if (acceptIcon != null)
                    actionButtonIcon.sprite = acceptIcon;
                actionButtonIcon.color = acceptIconColor;
                break;
        }
    }

    // НОВОЕ: клик по всей строке
    private void HandleRowClick()
    {
        Debug.Log($"[FriendItemView] Row clicked, friendUid={friendUid}");
        // можно просто использовать отдельное событие
        OnRowClicked?.Invoke(friendUid);

        // или, если хочешь, чтобы клик по строке тоже считался "открыть профиль":
        // OnProfileClicked?.Invoke(friendUid);
    }

    private void HandleButtonClick()
    {
        switch (friendStatus)
        {
            case "active":
                OnProfileClicked?.Invoke(friendUid);
                break;

            case "pending_sent":
                OnRemoveClicked?.Invoke(friendUid);
                break;

            case "pending_received":
                OnAcceptClicked?.Invoke(friendUid);
                break;
        }
    }

    private IEnumerator LoadAvatar(string url)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (avatar != null)
                {
                    avatar.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }
            else
            {
                SetDefaultAvatar();
            }
        }
    }

    private void SetDefaultAvatar()
    {
        if (avatar != null)
            avatar.color = Color.gray;
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveAllListeners();

        if (rowButton != null)
            rowButton.onClick.RemoveAllListeners();
    }
}
