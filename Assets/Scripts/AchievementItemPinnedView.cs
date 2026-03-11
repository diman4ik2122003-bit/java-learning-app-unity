using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AchievementItemPinnedView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private Coroutine _loadCoroutine;
    private string _lastLoadedUrl;
    private TokenManager.Achievement _cachedAchievement;

    public void Bind(TokenManager.Achievement achievement, TokenManager.UserAchievement userAchievement)
    {
        if (achievement == null)
        {
            Debug.LogWarning("[AchievementItemPinnedView] achievement is null");
            return;
        }

        _cachedAchievement = achievement;

        UpdateTitle(LocalizationManager.Instance?.CurrentLang ?? "ru");

        if (iconImage != null && !string.IsNullOrEmpty(achievement.iconUnlocked))
        {
            if (achievement.iconUnlocked != _lastLoadedUrl)
            {
                if (_loadCoroutine != null)
                    StopCoroutine(_loadCoroutine);

                _loadCoroutine = StartCoroutine(LoadIcon(achievement.iconUnlocked));
            }
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        if (enableDebugLogs)
            Debug.Log($"[AchievementItemPinnedView] Bound: {achievement.id}");
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string lang)
    {
        UpdateTitle(lang);
    }

    private void UpdateTitle(string lang)
    {
        if (titleText == null || _cachedAchievement == null) return;
        titleText.text = _cachedAchievement.title?.GetText(lang) ?? "";
    }

    private IEnumerator LoadIcon(string url)
    {
        if (_spriteCache.TryGetValue(url, out var cached) && cached != null)
        {
            iconImage.sprite = cached;
            iconImage.color = Color.white;
            _lastLoadedUrl = url;
            _loadCoroutine = null;
            yield break;
        }

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                if (tex != null && iconImage != null)
                {
                    Sprite sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    _spriteCache[url] = sprite;
                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                    _lastLoadedUrl = url;

                    if (enableDebugLogs)
                        Debug.Log($"[AchievementItemPinnedView] Icon loaded: {url}");
                }
            }
            else
            {
                Debug.LogWarning($"[AchievementItemPinnedView] Failed to load icon: {uwr.error}");
                if (iconImage != null)
                    iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }

        _loadCoroutine = null;
    }
}
