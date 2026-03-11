using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class PinnedAchievementsPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject achievementRowPrefab;      // ← Префаб строки
    [SerializeField] private GameObject achievementItemPrefab;     // ← Префаб айтема

    [Header("Root")]
    [SerializeField] private ScrollRect scrollRect;                // ← ScrollRect (можно повесить скрипт на него)
    [SerializeField] private Transform pinnedContainer;            // ← Content внутри ScrollRect

    [Header("Empty State (optional)")]
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Settings")]
    [SerializeField] private int maxPinnedAchievements = 3;
    [SerializeField] private int achievementsPerRow = 3;           // ← Сколько ачивок в одной строке

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private TokenManager.UserAchievement[] allUserAchievements;
    private TokenManager.Achievement[] allAchievements;

    private void Awake()
    {
        // Автоматически найти ScrollRect и Content, если не заданы
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (pinnedContainer == null && scrollRect != null)
            pinnedContainer = scrollRect.content;
    }

    /// <summary>
    /// Применяет данные закрепленных достижений к панели
    /// </summary>
    public void Apply(
        TokenManager.AchievementListResponse achievementsResp,
        TokenManager.UserAchievementListResponse userAchievementsResp)
    {
        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Apply called");

        if (!pinnedContainer || !achievementRowPrefab || !achievementItemPrefab)
        {
            Debug.LogError("[PinnedAchievementsPanelBinder] References not set. " +
                          $"Container: {pinnedContainer != null}, " +
                          $"RowPrefab: {achievementRowPrefab != null}, " +
                          $"ItemPrefab: {achievementItemPrefab != null}");
            return;
        }

        if (achievementsResp?.data == null || userAchievementsResp?.data == null)
        {
            if (debugLogs)
                Debug.LogWarning("[PinnedAchievementsPanelBinder] No achievements data");

            ClearContent();
            ShowEmptyState(true);
            return;
        }

        allAchievements = achievementsResp.data;
        allUserAchievements = userAchievementsResp.data;

        // Фильтруем только закрепленные достижения
        var pinnedAchievements = allUserAchievements
            .Where(ua => ua.isPinned)
            .OrderBy(ua => ua.pinOrder)
            .Take(maxPinnedAchievements)
            .ToList();

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Found {pinnedAchievements.Count} pinned achievements");

        ClearContent();

        if (pinnedAchievements.Count == 0)
        {
            ShowEmptyState(true);
            return;
        }

        ShowEmptyState(false);

        // Создаем строки с достижениями (по achievementsPerRow штук в строке)
        for (int i = 0; i < pinnedAchievements.Count; i += achievementsPerRow)
        {
            var rowAchievements = pinnedAchievements
                .Skip(i)
                .Take(achievementsPerRow)
                .ToList();

            CreateAchievementRow(rowAchievements);
        }

        // Обновляем layout
        Canvas.ForceUpdateCanvases();
        if (pinnedContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Displayed {pinnedAchievements.Count} pinned achievements");
    }

    /// <summary>
    /// Создает строку с достижениями
    /// </summary>
    private void CreateAchievementRow(System.Collections.Generic.List<TokenManager.UserAchievement> userAchievements)
    {
        GameObject rowObj = Instantiate(achievementRowPrefab, pinnedContainer, false);
        NormalizeRect(rowObj.transform);

        // Контейнер для айтемов внутри строки (должен быть в префабе)
        Transform rowContainer = rowObj.transform;

        foreach (var userAch in userAchievements)
        {
            var achDef = allAchievements.FirstOrDefault(a => a.id == userAch.id);
            if (achDef == null)
            {
                if (debugLogs)
                    Debug.LogWarning($"[PinnedAchievementsPanelBinder] Achievement {userAch.id} not found");
                continue;
            }

            CreateAchievementItem(rowContainer, achDef, userAch);
        }

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Created row with {userAchievements.Count} achievements");
    }

    /// <summary>
    /// Создает UI элемент достижения
    /// </summary>
    private void CreateAchievementItem(
        Transform parent, 
        TokenManager.Achievement achievement, 
        TokenManager.UserAchievement userAchievement)
    {
        GameObject itemObj = Instantiate(achievementItemPrefab, parent, false);
        NormalizeRect(itemObj.transform);

        // Получаем текущий язык
        string currentLang = LocalizationManager.Instance?.CurrentLang ?? "ru";

        // Находим компоненты в префабе
        Image iconImage = itemObj.transform.Find("Icon")?.GetComponent<Image>();
        TextMeshProUGUI titleText = itemObj.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = itemObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();

        // Заполняем данные
        if (titleText != null)
        {
            titleText.text = achievement.title?.GetText(currentLang) ?? "Achievement";
        }

        if (descriptionText != null)
        {
            descriptionText.text = achievement.description?.GetText(currentLang) ?? "";
        }

        if (iconImage != null && !string.IsNullOrEmpty(achievement.iconUnlocked))
        {
            StartCoroutine(LoadAchievementIcon(iconImage, achievement.iconUnlocked));
        }

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Created achievement: {achievement.id}");
    }

    /// <summary>
    /// Загружает иконку достижения
    /// </summary>
    private IEnumerator LoadAchievementIcon(Image iconImage, string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            iconImage.sprite = sprite;

            if (debugLogs)
                Debug.Log($"[PinnedAchievementsPanelBinder] Icon loaded successfully");
        }
        else
        {
            Debug.LogError($"[PinnedAchievementsPanelBinder] Failed to load icon: {request.error}");
        }
    }

    /// <summary>
    /// Очищает контент
    /// </summary>
    private void ClearContent()
    {
        if (pinnedContainer == null) return;

        int childCount = pinnedContainer.childCount;

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Clearing content, children: {childCount}");

        for (int i = pinnedContainer.childCount - 1; i >= 0; i--)
        {
            var child = pinnedContainer.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
#else
            Destroy(child);
#endif
        }

        if (debugLogs)
            Debug.Log($"[PinnedAchievementsPanelBinder] Content cleared");
    }

    /// <summary>
    /// Показывает/скрывает пустое состояние
    /// </summary>
    private void ShowEmptyState(bool show)
    {
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(show);

            if (show && emptyStateText != null)
            {
                string currentLang = LocalizationManager.Instance?.CurrentLang ?? "ru";
                emptyStateText.text = currentLang == "en" 
                    ? "No pinned achievements yet" 
                    : "Нет закрепленных достижений";
            }
        }
    }

    /// <summary>
    /// Нормализует RectTransform
    /// </summary>
    private static void NormalizeRect(Transform t)
    {
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;

        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
        else
            t.localPosition = Vector3.zero;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Fill Pinned Achievements")]
    private void TestFillPinnedAchievements()
    {
        Debug.Log("[PinnedAchievementsPanelBinder] ========== TEST START ==========");

        var testAchievements = new TokenManager.AchievementListResponse
        {
            data = new TokenManager.Achievement[]
            {
                new TokenManager.Achievement
                {
                    id = "ach1",
                    title = new TokenManager.LocalizedString { ru = "Первые шаги", en = "First Steps" },
                    description = new TokenManager.LocalizedString { ru = "Завершите первый урок", en = "Complete first lesson" },
                    iconUnlocked = "",
                    rewardXp = 10
                },
                new TokenManager.Achievement
                {
                    id = "ach2",
                    title = new TokenManager.LocalizedString { ru = "Код-мастер", en = "Code Master" },
                    description = new TokenManager.LocalizedString { ru = "Решите 10 задач", en = "Solve 10 challenges" },
                    iconUnlocked = "",
                    rewardXp = 50
                },
                new TokenManager.Achievement
                {
                    id = "ach3",
                    title = new TokenManager.LocalizedString { ru = "Стример знаний", en = "Knowledge Streaker" },
                    description = new TokenManager.LocalizedString { ru = "Учитесь 7 дней подряд", en = "Study 7 days in a row" },
                    iconUnlocked = "",
                    rewardXp = 100
                }
            }
        };

        var testUserAchievements = new TokenManager.UserAchievementListResponse
        {
            data = new TokenManager.UserAchievement[]
            {
                new TokenManager.UserAchievement
                {
                    id = "ach1",
                    isPinned = true,
                    pinOrder = 0,
                    unlockedAt = 1234567890
                },
                new TokenManager.UserAchievement
                {
                    id = "ach2",
                    isPinned = true,
                    pinOrder = 1,
                    unlockedAt = 1234567891
                },
                new TokenManager.UserAchievement
                {
                    id = "ach3",
                    isPinned = true,
                    pinOrder = 2,
                    unlockedAt = 1234567892
                }
            }
        };

        Apply(testAchievements, testUserAchievements);

        Debug.Log("[PinnedAchievementsPanelBinder] ========== TEST END ==========");
    }
#endif
}
