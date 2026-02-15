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


    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;


    private Button _button;
    private string _achievementId;
    private bool _isPinned;
    private bool _unlocked;
    private bool _isProcessing;


    private void Awake()
    {
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== AWAKE START ========== GameObject: {gameObject.name}");


        _button = GetComponent<Button>();
        
        if (_button == null)
        {
            Debug.LogError($"[AchievementItemView] ❌ NO BUTTON COMPONENT on '{gameObject.name}'! Add Button component to prefab!");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] ✅ Button FOUND on '{gameObject.name}'");
        }


        if (pinIcon == null)
        {
            Debug.LogWarning($"[AchievementItemView] ⚠️ pinIcon is NULL on '{gameObject.name}'! Assign PinImage in Inspector!");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] ✅ pinIcon assigned: {pinIcon.name}, active={pinIcon.activeSelf}");
        }


        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] ========== AWAKE END ==========");
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
            if (enableDebugLogs)
                Debug.Log($"[AchievementItemView] Button was null, trying GetComponent...");
            
            _button = GetComponent<Button>();
            
            if (_button == null)
                Debug.LogError($"[AchievementItemView] ❌ Still no Button after GetComponent!");
            else
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] ✅ Button found via GetComponent");
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


        // ========== ИСПРАВЛЕННЫЙ БЛОК! ⬇️⬇️⬇️ ==========
        if (iconImage)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] Starting to load image from: {imageUrl}");
                
                StartCoroutine(LoadImageFromUrl(imageUrl));
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[AchievementItemView] No imageUrl, keeping prefab sprite");
                
                // НЕ ТРОГАЕМ iconImage.sprite и iconImage.color!
                // Останется дефолтный спрайт из префаба ✅
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
                    Debug.Log($"[AchievementItemView] ✅ Listener ADDED! interactable={_button.interactable} (isPinned={isPinned} || canPin={canPin})");
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
            Debug.LogError($"[AchievementItemView] ❌ NO BUTTON found on '{gameObject.name}' — clicks won't work!");
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


        if (_isPinned)
        {
            Debug.Log($"[AchievementItemView] Starting UNPIN coroutine...");
            StartCoroutine(DoUnpin());
        }
        else
        {
            Debug.Log($"[AchievementItemView] Starting PIN coroutine...");
            StartCoroutine(DoPin());
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
            
            // ✅ ОБНОВЛЯЕМ ЛОКАЛЬНО!
            Debug.Log($"[AchievementItemView] Setting _isPinned = true");
            _isPinned = true;
            
            Debug.Log($"[AchievementItemView] Calling UpdatePinIcon()...");
            UpdatePinIcon();
            
            // Обновляем данные в TokenManager
            Debug.Log($"[AchievementItemView] Calling RefreshUserAchievements...");
            TokenManager.Instance.RefreshUserAchievements();
            
            // Можно оставить кнопку активной для unpin
            if (_button)
            {
                _button.interactable = true;
                Debug.Log($"[AchievementItemView] Button re-enabled");
            }


            Debug.Log($"[AchievementItemView] ========== DoPin END (SUCCESS) ==========");
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] ❌ PIN FAILED: {error}");
            
            if (_button)
            {
                _button.interactable = true;
                Debug.Log($"[AchievementItemView] Button re-enabled after failure");
            }


            Debug.Log($"[AchievementItemView] ========== DoPin END (FAILED) ==========");
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
            Debug.Log($"[AchievementItemView] ✅ UNPIN SUCCESS!");
            
            // ✅ ОБНОВЛЯЕМ ЛОКАЛЬНО!
            Debug.Log($"[AchievementItemView] Setting _isPinned = false");
            _isPinned = false;
            
            Debug.Log($"[AchievementItemView] Calling UpdatePinIcon()...");
            UpdatePinIcon();
            
            // Обновляем данные в TokenManager
            Debug.Log($"[AchievementItemView] Calling RefreshUserAchievements...");
            TokenManager.Instance.RefreshUserAchievements();
            
            if (_button)
            {
                _button.interactable = true;
                Debug.Log($"[AchievementItemView] Button re-enabled");
            }


            Debug.Log($"[AchievementItemView] ========== DoUnpin END (SUCCESS) ==========");
        }
        else
        {
            Debug.LogWarning($"[AchievementItemView] ❌ UNPIN FAILED: {error}");
            
            if (_button)
            {
                _button.interactable = true;
                Debug.Log($"[AchievementItemView] Button re-enabled after failure");
            }


            Debug.Log($"[AchievementItemView] ========== DoUnpin END (FAILED) ==========");
        }
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


    private IEnumerator LoadImageFromUrl(string url)
    {
        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] LoadImageFromUrl START: {url}");


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


                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;


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


        if (enableDebugLogs)
            Debug.Log($"[AchievementItemView] LoadImageFromUrl END");
    }
}
