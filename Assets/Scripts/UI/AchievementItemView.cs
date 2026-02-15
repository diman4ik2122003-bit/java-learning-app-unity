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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

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

        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== AWAKE END ==========");
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

        // ========== ИСПРАВЛЕННЫЙ БЛОК С КАРТИНКОЙ! ⬇️⬇️⬇️ ==========
        if (iconImage)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Load icon — skip if same URL already loaded
                if (imageUrl != _lastLoadedUrl)
                {
                    if (_loadCoroutine != null)
                        StopCoroutine(_loadCoroutine);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[AchievementItemView] Starting to load image from: {imageUrl}");
                    
                    _loadCoroutine = StartCoroutine(LoadImageFromUrl(imageUrl));
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] No imageUrl, keeping prefab sprite");
                
                // НЕ ТРОГАЕМ iconImage.sprite! Останется дефолтный из префаба ✅
            }
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
                
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] ✅ Listener ADDED! interactable={_button.interactable}");
            }
            else
            {
                _button.interactable = false;
                
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] Button disabled (not unlocked)");
            }
        }
        else
        {
            Debug.LogError($"[AchievementItemView] ❌ NO BUTTON found on '{gameObject.name}'!");
        }

        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== BIND END ==========");
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
        Debug.Log($"[AchievementItemView] ========== CLICKED! ==========");
        Debug.Log($"[AchievementItemView] id='{_achievementId}', unlocked={_unlocked}, isPinned={_isPinned}, processing={_isProcessing}");

        if (_isProcessing)
        {
            Debug.LogWarning("[AchievementItemView] ⚠️ Click IGNORED: already processing");
            return;
        }
        if (!_unlocked)
        {
            Debug.LogWarning("[AchievementItemView] ⚠️ Click IGNORED: not unlocked");
            return;
        }
        if (string.IsNullOrEmpty(_achievementId))
        {
            Debug.LogWarning("[AchievementItemView] ⚠️ Click IGNORED: achievementId is null/empty");
            return;
        }
        if (TokenManager.Instance == null)
        {
            Debug.LogWarning("[AchievementItemView] ⚠️ Click IGNORED: TokenManager.Instance is null");
            return;
        }

        _isProcessing = true;
        if (_button) _button.interactable = false;

        Debug.Log($"[AchievementItemView] Processing click... isPinned={_isPinned}");

        // Optimistic visual update — toggle pin icon immediately
        bool newPinnedState = !_isPinned;
        _isPinned = newPinnedState;
        if (pinIcon) pinIcon.SetActive(_isPinned);

        if (_isPinned)
        {
            Debug.Log($"[AchievementItemView] Starting PIN coroutine...");
            StartCoroutine(DoPin());
        }
        else
        {
            Debug.Log($"[AchievementItemView] Starting UNPIN coroutine...");
            StartCoroutine(DoUnpin());
        }
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
            Debug.Log($"[AchievementItemView] ✅ PIN SUCCESS!");
            UpdateLocalPinState(true);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] ❌ PIN FAILED: {error}");
            
            // Rollback optimistic update
            _isPinned = false;
            if (pinIcon) pinIcon.SetActive(false);
            if (_button) _button.interactable = true;
        }

        Debug.Log($"[AchievementItemView] ========== DoPin END ==========");
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
            Debug.Log($"[AchievementItemView] ✅ UNPIN SUCCESS!");
            UpdateLocalPinState(false);
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] ❌ UNPIN FAILED: {error}");
            
            // Rollback optimistic update
            _isPinned = true;
            if (pinIcon) pinIcon.SetActive(true);
            if (_button) _button.interactable = true;
        }

        Debug.Log($"[AchievementItemView] ========== DoUnpin END ==========");
    }


    /// <summary>
    /// Обновляет видимость булавки
    /// </summary>
    private void UpdatePinIcon()
    {
        Debug.Log($"[AchievementItemView] ========== UpdatePinIcon START ==========");
        Debug.Log($"[AchievementItemView] _isPinned = {_isPinned}");
        
        if (pinIcon == null)
        {
            Debug.LogError($"[AchievementItemView] ❌ pinIcon is NULL! Cannot update visibility!");
            Debug.Log($"[AchievementItemView] ========== UpdatePinIcon END (FAILED) ==========");
            return;
        }

        Debug.Log($"[AchievementItemView] pinIcon exists: {pinIcon.name}");
        Debug.Log($"[AchievementItemView] pinIcon.activeSelf BEFORE: {pinIcon.activeSelf}");
        
        pinIcon.SetActive(_isPinned);
        
        Debug.Log($"[AchievementItemView] pinIcon.SetActive({_isPinned}) called");
        Debug.Log($"[AchievementItemView] pinIcon.activeSelf AFTER: {pinIcon.activeSelf}");
        
        if (_isPinned)
            Debug.Log($"[AchievementItemView] ✅ PinIcon should be VISIBLE now!");
        else
            Debug.Log($"[AchievementItemView] ❌ PinIcon should be HIDDEN now!");

        Debug.Log($"[AchievementItemView] ========== UpdatePinIcon END ==========");
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
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] LoadImageFromUrl START: {url}");

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
            
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] ✅ Image loaded from cache!");
            
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

                    if (enableDebugLogs)
                        Debug.Log($"[AchievementItemView] ✅ Image loaded successfully: {texture.width}x{texture.height}");
                }
                else
                {
                    Debug.LogWarning($"[AchievementItemView] texture or iconImage is null after download");
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
        
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] LoadImageFromUrl END");
    }
}
