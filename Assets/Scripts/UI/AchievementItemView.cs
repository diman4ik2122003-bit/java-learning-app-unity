using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class AchievementItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Loading")]
    [SerializeField] private CanvasGroup loadingSpinner;

    [Header("Pin")]
    [SerializeField] private GameObject pinIcon;  // 📌 image, hidden when unpinned

    private Button _button;
    private string _achievementId;
    private bool _isPinned;
    private bool _unlocked;
    private bool _isProcessing;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void Bind(string achievementId, string title, string description,
                     string imageUrl, bool unlocked, bool isPinned, bool canPin)
    {
        _achievementId = achievementId;
        _isPinned = isPinned;
        _unlocked = unlocked;

        if (titleText)
            titleText.text = title ?? "";

        if (descriptionText)
            descriptionText.text = description ?? "";

        if (iconImage)
        {
            if (!string.IsNullOrEmpty(imageUrl))
                StartCoroutine(LoadImageFromUrl(imageUrl));
            else
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }

        // Show pin icon only when pinned
        if (pinIcon)
            pinIcon.SetActive(isPinned);

        // Whole item is clickable for pin/unpin (only for unlocked achievements)
        if (_button)
        {
            _button.onClick.RemoveAllListeners();

            if (unlocked)
            {
                _button.interactable = isPinned || canPin;
                _button.onClick.AddListener(OnItemClicked);
            }
            else
            {
                _button.interactable = false;
            }
        }

        Debug.Log($"[AchievementItemView] Bound: id='{achievementId}', title='{title}', unlocked={unlocked}, isPinned={isPinned}, canPin={canPin}");
    }

    // Backward-compatible overload
    public void Bind(string title, string description, string imageUrl, bool unlocked = true)
    {
        Bind(null, title, description, imageUrl, unlocked, false, false);
    }

    private void OnItemClicked()
    {
        if (_isProcessing || !_unlocked || string.IsNullOrEmpty(_achievementId)) return;
        if (TokenManager.Instance == null) return;

        _isProcessing = true;
        if (_button) _button.interactable = false;

        if (_isPinned)
            StartCoroutine(DoUnpin());
        else
            StartCoroutine(DoPin());
    }

    private IEnumerator DoPin()
    {
        bool success = false;
        string error = null;

        yield return TokenManager.Instance.PinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error = err;
        });

        _isProcessing = false;

        if (success)
        {
            Debug.Log($"[AchievementItemView] Pinned {_achievementId}");
            TokenManager.Instance.RefreshUserAchievements();
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] Pin failed: {error}");
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator DoUnpin()
    {
        bool success = false;
        string error = null;

        yield return TokenManager.Instance.UnpinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error = err;
        });

        _isProcessing = false;

        if (success)
        {
            Debug.Log($"[AchievementItemView] Unpinned {_achievementId}");
            TokenManager.Instance.RefreshUserAchievements();
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] Unpin failed: {error}");
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        if (loadingSpinner)
            loadingSpinner.alpha = 1f;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (loadingSpinner)
                loadingSpinner.alpha = 0f;

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                if (texture != null && iconImage != null)
                {
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogError($"[AchievementItemView] Failed to load image from {url}: {uwr.error}");

                if (iconImage)
                    iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }
    }
}
