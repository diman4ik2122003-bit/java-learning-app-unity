using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LeaderboardItemView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txtRank;
    [SerializeField] private TextMeshProUGUI txtDisplayName;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtXp;
    [SerializeField] private Image avatarImage;
    
    [Header("Highlight Settings")]
    [SerializeField] private TMP_FontAsset normalFont; // Обычный шрифт
    [SerializeField] private TMP_FontAsset highlightFont; // alagard-12px-unicode SDF 2
    [SerializeField] private Color normalTextColor = new Color(222f/255f, 176f/255f, 120f/255f, 1f); // Бежевый
    [SerializeField] private Color highlightTextColor = new Color(222f/255f, 165f/255f, 60f/255f, 1f); // Золотой
    
    [Header("Avatar Settings")]
    [SerializeField] private Sprite defaultAvatarSprite;
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private string _pendingAvatarUrl;
    private string _pendingPlayerName;

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(_pendingAvatarUrl))
        {
            if (debugLogs)
                Debug.Log($"[LeaderboardItemView] OnEnable: loading deferred avatar for {_pendingPlayerName}");
            StartCoroutine(LoadAvatar(_pendingAvatarUrl, _pendingPlayerName));
            _pendingAvatarUrl = null;
            _pendingPlayerName = null;
        }
    }

    public void Bind(
        int rank,
        string displayName,
        string discriminator,
        int level,
        int xp,
        string photoURL,
        bool isCurrentUser = false)
    {
        if (debugLogs)
        {
            Debug.Log($"[LeaderboardItemView] Bind START: rank={rank}, name={displayName}, isCurrentUser={isCurrentUser}");
        }

        // Заполняем текстовые поля
        if (txtRank) txtRank.text = $"#{rank}";
        if (txtDisplayName) txtDisplayName.text = displayName;
        if (txtLevel) txtLevel.text = $"Lvl {level}";
        if (txtXp) txtXp.text = $"{xp:N0} XP";

        // Определяем стили для текущего пользователя
        TMP_FontAsset fontToUse = isCurrentUser ? highlightFont : normalFont;
        Color textColor = isCurrentUser ? highlightTextColor : normalTextColor;
        FontStyles fontStyle = isCurrentUser ? FontStyles.Bold : FontStyles.Normal;

        // Применяем к Rank
        if (txtRank)
        {
            if (fontToUse) txtRank.font = fontToUse;
            txtRank.color = textColor;
            txtRank.fontStyle = fontStyle;
        }

        // Применяем к DisplayName
        if (txtDisplayName)
        {
            if (fontToUse) txtDisplayName.font = fontToUse;
            txtDisplayName.color = textColor;
            txtDisplayName.fontStyle = fontStyle;
        }

        // Применяем к Level
        if (txtLevel)
        {
            if (fontToUse) txtLevel.font = fontToUse;
            txtLevel.color = textColor;
            txtLevel.fontStyle = fontStyle;
        }

        // Применяем к XP
        if (txtXp)
        {
            if (fontToUse) txtXp.font = fontToUse;
            txtXp.color = textColor;
            txtXp.fontStyle = fontStyle;
        }

        if (debugLogs)
        {
            Debug.Log($"[LeaderboardItemView] {displayName}: isCurrentUser={isCurrentUser}, " +
                      $"font={fontToUse?.name}, color=RGB({textColor.r * 255:F0}, {textColor.g * 255:F0}, {textColor.b * 255:F0}), " +
                      $"fontStyle={fontStyle}");
        }

        // Загрузка аватара
        if (avatarImage)
        {
            if (!string.IsNullOrEmpty(photoURL))
            {
                if (gameObject.activeInHierarchy)
                {
                    if (debugLogs)
                        Debug.Log($"[LeaderboardItemView] Starting LoadAvatar for {displayName}, URL: {photoURL}");
                    _pendingAvatarUrl = null;
                    StartCoroutine(LoadAvatar(photoURL, displayName));
                }
                else
                {
                    if (debugLogs)
                        Debug.Log($"[LeaderboardItemView] Deferring LoadAvatar for {displayName} (object inactive)");
                    _pendingAvatarUrl = photoURL;
                    _pendingPlayerName = displayName;
                }
            }
            else
            {
                if (debugLogs)
                    Debug.LogWarning($"[LeaderboardItemView] photoURL is EMPTY for {displayName}");
                
                if (defaultAvatarSprite)
                    avatarImage.sprite = defaultAvatarSprite;
            }
        }
    }

    private IEnumerator LoadAvatar(string url, string playerName)
    {
        if (debugLogs)
            Debug.Log($"[LeaderboardItemView] LoadAvatar START for {playerName}: {url}");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LeaderboardItemView] LoadAvatar FAILED for {playerName}: {www.error}");
                
                if (defaultAvatarSprite && avatarImage)
                    avatarImage.sprite = defaultAvatarSprite;
                
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            
            if (texture == null)
            {
                Debug.LogError($"[LeaderboardItemView] Texture is NULL for {playerName}!");
                yield break;
            }

            if (debugLogs)
                Debug.Log($"[LeaderboardItemView] Texture loaded for {playerName}: {texture.width}x{texture.height}");

            if (avatarImage == null)
            {
                Debug.LogError($"[LeaderboardItemView] avatarImage became NULL during loading for {playerName}!");
                yield break;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            avatarImage.sprite = sprite;
            avatarImage.enabled = true;

            if (debugLogs)
                Debug.Log($"[LeaderboardItemView] Avatar sprite SET for {playerName}!");
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
