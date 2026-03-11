using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Вешается на префаб AchievementItemPinned.
/// Заполняет иконку и название закреплённой ачивки.
/// Аналог AchievementItemView, но без кнопки пина — только отображение.
/// </summary>
public class AchievementItemPinnedView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Общий кэш спрайтов — не грузит одно изображение дважды
    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private Coroutine _loadCoroutine;
    private string _lastLoadedUrl;

    /// <summary>
    /// Заполняет элемент данными ачивки.
    /// </summary>
    public void Bind(TokenManager.Achievement achievement, TokenManager.UserAchievement userAchievement)
    {
        if (achievement == null)
        {
            Debug.LogWarning("[AchievementItemPinnedView] achievement is null");
            return;
        }

        string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";

        if (titleText != null)
            titleText.text = achievement.title?.GetText(lang) ?? "";

        if (iconImage != null && !string.IsNullOrEmpty(achievement.iconUnlocked))
        {
            if (achievement.iconUnlocked != _lastLoadedUrl)
            {
                if (_loadCoroutine != null)
                    StopCoroutine(_loadCoroutine);

                _loadCoroutine = StartCoroutine(LoadIcon(achievement.iconUnlocked));
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[AchievementItemPinnedView] Bound: {achievement.id}");
    }

    private IEnumerator LoadIcon(string url)
    {
        // Сначала проверяем кэш — мгновенный рендер без мигания
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
