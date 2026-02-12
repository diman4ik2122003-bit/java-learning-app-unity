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
        Debug.Log($"[AchievementItemView] Awake: button={(_button != null ? "FOUND" : "NULL")}, gameObject={gameObject.name}");
    }

    public void Bind(string achievementId, string title, string description,
                     string imageUrl, bool unlocked, bool isPinned, bool canPin)
    {
        // Fallback: get button if Awake hasn't run yet
        if (_button == null)
            _button = GetComponent<Button>();

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
                Debug.Log($"[AchievementItemView] Listener ADDED: id='{achievementId}', interactable={_button.interactable}");
            }
            else
            {
                _button.interactable = false;
            }
        }
        else
        {
            Debug.LogError($"[AchievementItemView] NO BUTTON found on '{gameObject.name}' — clicks won't work!");
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
        Debug.Log($"[AchievementItemView] CLICKED: id='{_achievementId}', unlocked={_unlocked}, isPinned={_isPinned}, processing={_isProcessing}");

        if (_isProcessing)
        {
            Debug.LogWarning("[AchievementItemView] Click ignored: already processing");
            return;
        }
        if (!_unlocked)
        {
            Debug.LogWarning("[AchievementItemView] Click ignored: not unlocked");
            return;
        }
        if (string.IsNullOrEmpty(_achievementId))
        {
            Debug.LogWarning("[AchievementItemView] Click ignored: achievementId is null/empty");
            return;
        }
        if (TokenManager.Instance == null)
        {
            Debug.LogWarning("[AchievementItemView] Click ignored: TokenManager.Instance is null");
            return;
        }

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
