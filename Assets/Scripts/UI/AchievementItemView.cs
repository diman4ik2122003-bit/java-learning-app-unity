using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    // Static sprite cache — survives item destruction, makes re-render instant
    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private Button _button;
    private string _achievementId;
    private bool _isPinned;
    private bool _unlocked;
    private bool _isProcessing;
    private string _lastLoadedUrl;
    private Coroutine _loadCoroutine;

    private void Awake()
    {
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== AWAKE START ========== GameObject: {gameObject.name}");


        _button = GetComponent<Button>();
        EnsureClickableArea();
    }

    /// <summary>
    /// Ensures the root GameObject has a transparent Image that covers the full area
    /// so the Button can receive clicks everywhere, not just on child graphics.
    /// Also disables raycastTarget on child graphics to prevent them from blocking.
    /// </summary>
    private void EnsureClickableArea()
    {
        // Add transparent Image on root if none exists
        var rootImage = GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
            rootImage.color = new Color(0, 0, 0, 0); // fully transparent
        }
        rootImage.raycastTarget = true;

        // Set this as the Button's target graphic
        if (_button != null)
            _button.targetGraphic = rootImage;

        // Disable raycast on all CHILD graphics so they don't block the root
        var childGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in childGraphics)
        {
            if (g.transform == transform) continue; // skip root
            g.raycastTarget = false;
        }
    }

    public void Bind(string achievementId, string title, string description,
                     string imageUrl, bool unlocked, bool isPinned, bool canPin)
    {
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== BIND START ==========");


        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] BIND PARAMS: id='{achievementId}', title='{title}', unlocked={unlocked}, isPinned={isPinned}, canPin={canPin}");


        // Fallback: get button if Awake hasn't run yet
        if (_button == null)
        {
            _button = GetComponent<Button>();
            EnsureClickableArea();
        }

        _achievementId = achievementId;
        _isPinned = isPinned;
        _unlocked = unlocked;


        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] Internal state set: _achievementId='{_achievementId}', _isPinned={_isPinned}, _unlocked={_unlocked}");


        if (titleText)
            titleText.text = title ?? "";
        else
            Debug.LogWarning($"[AchievementItemView] titleText is NULL!");


        if (descriptionText)
            descriptionText.text = description ?? "";
        else
            Debug.LogWarning($"[AchievementItemView] descriptionText is NULL!");


        // Load icon — skip if same URL already loaded
        if (iconImage && !string.IsNullOrEmpty(imageUrl) && imageUrl != _lastLoadedUrl)
        {
            if (_loadCoroutine != null)
                StopCoroutine(_loadCoroutine);
            _loadCoroutine = StartCoroutine(LoadImageFromUrl(imageUrl));
        }
        else if (iconImage && string.IsNullOrEmpty(imageUrl))
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1, 1, 1, 0.3f);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] iconImage is NULL!");
        }
        // ========== ИСПРАВЛЕННЫЙ БЛОК! ⬆️⬆️⬆️ ==========


        // ✅ Обновляем видимость булавки
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] Calling UpdatePinIcon()...");
        
        UpdatePinIcon();


        // Whole item is clickable for pin/unpin (only for unlocked achievements)
        if (_button)
        {
            _button.onClick.RemoveAllListeners();
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] Button listeners removed");


            if (unlocked)
            {
                _button.interactable = isPinned || canPin;
                _button.onClick.AddListener(OnItemClicked);
            }
            else
            {
                _button.interactable = false;
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] Button disabled (not unlocked)");
            }
        }
    }


    // Backward-compatible overload
    public void Bind(string title, string description, string imageUrl, bool unlocked = true)
    {
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] Bind (legacy overload) called with title='{title}'");
        
        Bind(null, title, description, imageUrl, unlocked, false, false);
    }


    private void OnItemClicked()
    {
        if (_isProcessing || !_unlocked || string.IsNullOrEmpty(_achievementId)) return;
        if (TokenManager.Instance == null) return;

        _isProcessing = true;
        if (_button) _button.interactable = false;

        // Optimistic visual update — toggle pin icon immediately
        _isPinned = !_isPinned;
        if (pinIcon) pinIcon.SetActive(_isPinned);

        if (_isPinned)
            StartCoroutine(DoPin());
        else
            StartCoroutine(DoUnpin());
    }


    private IEnumerator DoPin()
    {
        Debug.Log($"[AchievementItemView] ========== DoPin START ========== id='{_achievementId}'");


        bool success = false;
        string error = null;


        Debug.Log($"[AchievementItemView] Calling TokenManager.PinAchievement...");
        
        yield return TokenManager.Instance.PinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error = err;
            Debug.Log($"[AchievementItemView] PinAchievement callback: success={ok}, error='{err}'");
        });


        _isProcessing = false;
        Debug.Log($"[AchievementItemView] DoPin processing=false");


        if (success)
        {
            Debug.Log($"[AchievementItemView] Pinned {_achievementId}");
            UpdateLocalPinState(true);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] Pin failed: {error}");
            _isPinned = false;
            if (pinIcon) pinIcon.SetActive(false);
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator DoUnpin()
    {
        Debug.Log($"[AchievementItemView] ========== DoUnpin START ========== id='{_achievementId}'");


        bool success = false;
        string error = null;


        Debug.Log($"[AchievementItemView] Calling TokenManager.UnpinAchievement...");


        yield return TokenManager.Instance.UnpinAchievement(_achievementId, (ok, err) =>
        {
            success = ok;
            error = err;
            Debug.Log($"[AchievementItemView] UnpinAchievement callback: success={ok}, error='{err}'");
        });


        _isProcessing = false;
        Debug.Log($"[AchievementItemView] DoUnpin processing=false");


        if (success)
        {
            Debug.Log($"[AchievementItemView] Unpinned {_achievementId}");
            UpdateLocalPinState(false);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] Unpin failed: {error}");
            _isPinned = true;
            if (pinIcon) pinIcon.SetActive(true);
            if (_button) _button.interactable = true;
        }
    }

    /// <summary>
    /// Update achievementsMine locally and re-render without server round-trip.
    /// </summary>
    private void UpdateLocalPinState(bool pinned)
    {
        var tm = TokenManager.Instance;
        if (tm == null || tm.achievementsMine?.data == null) return;

        var mine = tm.achievementsMine.data;
        var entry = mine.FirstOrDefault(x => x.id == _achievementId);

        if (entry != null)
        {
            entry.isPinned = pinned;

            if (pinned)
            {
                // Assign next pinOrder
                int maxOrder = mine.Where(x => x.isPinned).Select(x => x.pinOrder).DefaultIfEmpty(0).Max();
                entry.pinOrder = maxOrder + 1;
            }
            else
            {
                entry.pinOrder = 0;
            }
        }

        // Re-render the panel with updated local data (no server fetch)
        tm.ApplyAchievementsToPanel();
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        // Check static cache first — instant render, no flicker
        if (_spriteCache.TryGetValue(url, out var cached) && cached != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = cached;
                iconImage.color = Color.white;
            }
            if (loadingSpinner) loadingSpinner.alpha = 0f;
            _lastLoadedUrl = url;
            _loadCoroutine = null;
            yield break;
        }

        if (loadingSpinner)
        {
            loadingSpinner.alpha = 1f;
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] Loading spinner enabled");
        }


        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();


            if (loadingSpinner)
            {
                loadingSpinner.alpha = 0f;
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] Loading spinner disabled");
            }


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

                    _spriteCache[url] = sprite; // Cache for future use
                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                    _lastLoadedUrl = url;
                }
            }
            else
            {
                Debug.LogError($"[AchievementItemView] ❌ Failed to load image from {url}: {uwr.error}");


                if (iconImage)
                    iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }

        _loadCoroutine = null;
    }
}
