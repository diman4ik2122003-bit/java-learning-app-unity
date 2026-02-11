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
    
    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.16f, 0.16f, 0.16f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f, 0.3f); // Gold
    
    [Header("Avatar Settings")]
    [SerializeField] private Sprite defaultAvatarSprite; // Опциональная дефолтная аватарка
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true; // ВКЛЮЧИ ДЛЯ ОТЛАДКИ


    public void Bind(
        int rank,
        string displayName,
        string discriminator,
        int level,
        int xp,
        string photoURL,
        bool isCurrentUser = false)
    {
        // Заполняем текстовые поля
        if (txtRank) txtRank.text = $"#{rank}";
        if (txtDisplayName) txtDisplayName.text = displayName; // Тег уже внутри
        if (txtLevel) txtLevel.text = $"Lvl {level}";
        if (txtXp) txtXp.text = $"{xp:N0} XP"; // Форматирование с разделителями

        // Подсветка текущего пользователя
        if (backgroundImage)
            backgroundImage.color = isCurrentUser ? highlightColor : normalColor;

        // Логирование
        if (debugLogs)
        {
            Debug.Log($"[LeaderboardItemView] Bind START: rank={rank}, name={displayName}, photoURL='{photoURL}', " +
                      $"avatarImage={avatarImage != null}, active={gameObject.activeSelf}");
        }

        // Загрузка аватара
        if (avatarImage)
        {
            if (!string.IsNullOrEmpty(photoURL))
            {
                if (debugLogs)
                    Debug.Log($"[LeaderboardItemView] Starting LoadAvatar for {displayName}");
                
                StartCoroutine(LoadAvatar(photoURL));
            }
            else
            {
                if (debugLogs)
                    Debug.LogWarning($"[LeaderboardItemView] photoURL is EMPTY for {displayName}");
                
                // Устанавливаем дефолтный спрайт
                if (defaultAvatarSprite)
                    avatarImage.sprite = defaultAvatarSprite;
            }
        }
        else
        {
            Debug.LogError("[LeaderboardItemView] avatarImage is NULL! Check prefab setup.");
        }
    }


    private IEnumerator LoadAvatar(string url)
    {
        if (debugLogs)
            Debug.Log($"[LeaderboardItemView] LoadAvatar START: {url}");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            // Отправляем запрос
            yield return www.SendWebRequest();

            // Проверяем ошибки
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LeaderboardItemView] LoadAvatar FAILED: {www.error} | URL: {url}");
                
                // Устанавливаем дефолтный спрайт при ошибке
                if (defaultAvatarSprite && avatarImage)
                    avatarImage.sprite = defaultAvatarSprite;
                
                yield break;
            }

            // Получаем текстуру
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            
            if (texture == null)
            {
                Debug.LogError("[LeaderboardItemView] Texture is NULL after download!");
                yield break;
            }

            if (debugLogs)
                Debug.Log($"[LeaderboardItemView] Texture loaded successfully: {texture.width}x{texture.height}");

            // Проверяем что компонент всё ещё существует
            if (avatarImage == null)
            {
                Debug.LogError("[LeaderboardItemView] avatarImage became NULL during loading!");
                yield break;
            }

            // Создаём спрайт из текстуры
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), // Pivot в центре
                100f // PixelsPerUnit
            );

            // Устанавливаем спрайт
            avatarImage.sprite = sprite;
            avatarImage.enabled = true; // Убедимся что Image включен

            if (debugLogs)
                Debug.Log($"[LeaderboardItemView] ✓ Avatar sprite SET! Size: {texture.width}x{texture.height}");
        }
    }


    // Опциональный метод для очистки при уничтожении
    private void OnDestroy()
    {
        // Останавливаем все корутины при уничтожении объекта
        StopAllCoroutines();
    }
}
