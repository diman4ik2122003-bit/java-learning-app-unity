using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AchievementItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup loadingSpinner;

    [Header("Pin")]
    [SerializeField] private GameObject pinIcon;

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private Button _button;
    private string _achievementId;
    private bool _unlocked;
    private bool _isPinned;
    private bool _isProcessing = false;

    private string _lastLoadedUrl;
    private string _pendingImageUrl;
    private Coroutine _loadCoroutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        EnsureClickableArea();
    }

    private void OnEnable()
    {
        // Retry image load that was deferred because the object was inactive during Bind()
        if (!string.IsNullOrEmpty(_pendingImageUrl) && _pendingImageUrl != _lastLoadedUrl)
        {
            _loadCoroutine = StartCoroutine(LoadImageFromUrl(_pendingImageUrl));
            _pendingImageUrl = null;
        }
    }

    private void EnsureClickableArea()
    {
        var rootImage = GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
            rootImage.color = new Color(0, 0, 0, 0);
        }
        rootImage.raycastTarget = true;

        if (_button != null)
            _button.targetGraphic = rootImage;

        var childGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in childGraphics)
        {
            if (g.transform == transform) continue;
            g.raycastTarget = false;
        }
    }

    public void Bind(string achievementId, string title, string description,
                     string imageUrl, bool unlocked, bool isPinned, bool canPin)
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
            EnsureClickableArea();
        }

        _achievementId = achievementId;
        _isPinned      = isPinned;
        _unlocked      = unlocked;

        if (titleText)       titleText.text       = title       ?? "";
        if (descriptionText) descriptionText.text = description ?? "";

        // Icon handling
        if (iconImage && !string.IsNullOrEmpty(imageUrl) && imageUrl != _lastLoadedUrl)
        {
            if (gameObject.activeInHierarchy)
            {
                _pendingImageUrl = null;
                if (_loadCoroutine != null) StopCoroutine(_loadCoroutine);
                _loadCoroutine = StartCoroutine(LoadImageFromUrl(imageUrl));
            }
            else
            {
                _pendingImageUrl = imageUrl;
                _lastLoadedUrl = null;
            }
        }

        if (pinIcon) pinIcon.SetActive(_isPinned);

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
        else
        {
            Debug.LogError($"[AchievementItemView] No Button on '{gameObject.name}'!");
        }
    }

    // Backward-compatible overload
    public void Bind(string title, string description, string imageUrl, bool unlocked = true)
        => Bind(null, title, description, imageUrl, unlocked, false, false);

    private void OnItemClicked()
    {
        if (_isProcessing || !_unlocked || string.IsNullOrEmpty(_achievementId)) return;
        if (TokenManager.Instance == null) return;

        _isProcessing = true;
        if (_button) _button.interactable = false;

        _isPinned = !_isPinned;
        if (pinIcon) pinIcon.SetActive(_isPinned);

        StartCoroutine(_isPinned ? DoPin() : DoUnpin());
    }

    private IEnumerator DoPin()
    {
        bool success = false;
        string error = null;

        yield return TokenManager.Instance.PinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error   = err;
        });

        _isProcessing = false;

        if (success)
        {
            UpdateLocalPinState(true);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] PIN FAILED: {error}");
            _isPinned = false;
            if (pinIcon)  pinIcon.SetActive(false);
            if (_button)  _button.interactable = true;
        }
    }

    private IEnumerator DoUnpin()
    {
        bool success = false;
        string error = null;

        yield return TokenManager.Instance.UnpinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error   = err;
        });

        _isProcessing = false;

        if (success)
        {
            UpdateLocalPinState(false);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] UNPIN FAILED: {error}");
            _isPinned = true;
            if (pinIcon)  pinIcon.SetActive(true);
            if (_button)  _button.interactable = true;
        }
    }

    private void UpdateLocalPinState(bool pinned)
    {
        var tm = TokenManager.Instance;
        if (tm == null || tm.achievementsMine?.data == null) return;

        var mine  = tm.achievementsMine.data;
        var entry = mine.FirstOrDefault(x => x.id == _achievementId);

        if (entry != null)
        {
            entry.isPinned = pinned;
            entry.pinOrder = pinned
                ? mine.Where(x => x.isPinned).Select(x => x.pinOrder).DefaultIfEmpty(0).Max() + 1
                : 0;
        }

        tm.ApplyAchievementsToPanel();
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        if (_spriteCache.TryGetValue(url, out var cached) && cached != null)
        {
            if (iconImage) { iconImage.sprite = cached; iconImage.color = Color.white; }
            if (loadingSpinner) loadingSpinner.alpha = 0f;
            _lastLoadedUrl = url;
            _loadCoroutine = null;
            yield break;
        }

        if (loadingSpinner) loadingSpinner.alpha = 1f;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (loadingSpinner) loadingSpinner.alpha = 0f;

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                if (texture != null && iconImage != null)
                {
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    _spriteCache[url]  = sprite;
                    iconImage.sprite   = sprite;
                    iconImage.color    = Color.white;
                    _lastLoadedUrl     = url;
                }
            }
            else
            {
                Debug.LogWarning($"[AchievementItemView] Failed to load image: {uwr.error}");
                if (iconImage) iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }

        _loadCoroutine = null;
    }
}
