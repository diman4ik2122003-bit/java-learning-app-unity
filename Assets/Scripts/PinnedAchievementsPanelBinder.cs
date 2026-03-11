using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PinnedAchievementsPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject achievementRowPrefab;       // AchievmentsPinnedRow
    [SerializeField] private GameObject achievementItemPrefab;      // AchievementItemPinned

    [Header("Root")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform pinnedContainer;             // Content внутри Achivs Scroll View

    [Header("Empty State (optional)")]
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Settings")]
    [SerializeField] private int maxPinnedAchievements = 3;
    [SerializeField] private int achievementsPerRow = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private TokenManager.UserAchievement[] allUserAchievements;
    private TokenManager.Achievement[] allAchievements;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (pinnedContainer == null && scrollRect != null)
            pinnedContainer = scrollRect.content;
    }

    public void Apply(
        TokenManager.AchievementListResponse achievementsResp,
        TokenManager.UserAchievementListResponse userAchievementsResp)
    {
        if (debugLogs) Debug.Log("[PinnedAchievementsPanelBinder] Apply called");

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
            if (debugLogs) Debug.LogWarning("[PinnedAchievementsPanelBinder] No achievements data");
            ClearContent();
            ShowEmptyState(true);
            return;
        }

        allAchievements = achievementsResp.data;
        allUserAchievements = userAchievementsResp.data;

        var pinnedAchievements = allUserAchievements
            .Where(ua => ua.isPinned)
            .OrderBy(ua => ua.pinOrder)
            .Take(maxPinnedAchievements)
            .ToList();

        if (debugLogs) Debug.Log($"[PinnedAchievementsPanelBinder] Found {pinnedAchievements.Count} pinned");

        ClearContent();

        if (pinnedAchievements.Count == 0)
        {
            ShowEmptyState(true);
            return;
        }

        ShowEmptyState(false);

        for (int i = 0; i < pinnedAchievements.Count; i += achievementsPerRow)
        {
            var rowSlice = pinnedAchievements.Skip(i).Take(achievementsPerRow).ToList();
            CreateRow(rowSlice);
        }

        // Принудительно перестраиваем layout после спавна
        Canvas.ForceUpdateCanvases();
        if (pinnedContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        if (debugLogs) Debug.Log("[PinnedAchievementsPanelBinder] Done");
    }

    // ─────────────────────────────────────────────
    //  Приватные методы
    // ─────────────────────────────────────────────

    private void CreateRow(List<TokenManager.UserAchievement> userAchievements)
    {
        GameObject rowObj = Instantiate(achievementRowPrefab, pinnedContainer, false);

        // ★ ГЛАВНЫЙ ФИКс: растягиваем строку на всю ширину Content
        StretchHorizontally(rowObj.GetComponent<RectTransform>());

        foreach (var userAch in userAchievements)
        {
            var achDef = allAchievements.FirstOrDefault(a => a.id == userAch.id);
            if (achDef == null)
            {
                if (debugLogs) Debug.LogWarning($"[PinnedAchievementsPanelBinder] Achievement def not found: {userAch.id}");
                continue;
            }

            CreateItem(rowObj.transform, achDef, userAch);
        }

        if (debugLogs) Debug.Log($"[PinnedAchievementsPanelBinder] Row created with {userAchievements.Count} items");
    }

    private void CreateItem(Transform parent, TokenManager.Achievement achievement, TokenManager.UserAchievement userAchievement)
    {
        GameObject itemObj = Instantiate(achievementItemPrefab, parent, false);

        // Биндим через специализированный View-скрипт
        var view = itemObj.GetComponent<AchievementItemPinnedView>();
        if (view != null)
        {
            view.Bind(achievement, userAchievement);
        }
        else
        {
            // Fallback — заполняем вручную если скрипт не навешен на префаб
            var titleText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
                titleText.text = achievement.title?.GetText(lang) ?? "";
            }
            if (debugLogs) Debug.LogWarning("[PinnedAchievementsPanelBinder] AchievementItemPinnedView not found on prefab, used fallback");
        }
    }

    /// <summary>
    /// Растягивает RectTransform на всю ширину родителя (anchorMin.x=0, anchorMax.x=1, offsetMin.x=0, offsetMax.x=0).
    /// Высоту не трогает — управляется LayoutElement на самом объекте.
    /// </summary>
    private static void StretchHorizontally(RectTransform rt)
    {
        if (rt == null) return;

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        // Stretch по горизонтали, сохраняем Y-якоря как были
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);

        // Обнуляем отступы по X (left=0, right=0)
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
    }

    private void ClearContent()
    {
        if (pinnedContainer == null) return;

        if (debugLogs) Debug.Log($"[PinnedAchievementsPanelBinder] Clearing {pinnedContainer.childCount} children");

        for (int i = pinnedContainer.childCount - 1; i >= 0; i--)
        {
            var child = pinnedContainer.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private void ShowEmptyState(bool show)
    {
        if (emptyStatePanel == null) return;

        emptyStatePanel.SetActive(show);

        if (show && emptyStateText != null)
        {
            string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
            emptyStateText.text = lang == "en"
                ? "No pinned achievements yet"
                : "Нет закреплённых достижений";
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Fill Pinned Achievements")]
private void TestFill()
{
    var testAch = new TokenManager.AchievementListResponse
    {
        data = new[]
        {
            new TokenManager.Achievement { id = "ach1",  title = new TokenManager.LocalizedString { ru = "Первые шаги",       en = "First Steps"        }, iconUnlocked = "", rewardXp = 10  },
            new TokenManager.Achievement { id = "ach2",  title = new TokenManager.LocalizedString { ru = "Код-мастер",        en = "Code Master"        }, iconUnlocked = "", rewardXp = 50  },
            new TokenManager.Achievement { id = "ach3",  title = new TokenManager.LocalizedString { ru = "Стример знаний",    en = "Knowledge Streaker" }, iconUnlocked = "", rewardXp = 100 },
            new TokenManager.Achievement { id = "ach4",  title = new TokenManager.LocalizedString { ru = "Быстрый старт",     en = "Quick Start"        }, iconUnlocked = "", rewardXp = 20  },
            new TokenManager.Achievement { id = "ach5",  title = new TokenManager.LocalizedString { ru = "Марафонец",         en = "Marathoner"         }, iconUnlocked = "", rewardXp = 75  },
            new TokenManager.Achievement { id = "ach6",  title = new TokenManager.LocalizedString { ru = "Мастер алгоритмов", en = "Algorithm Master"   }, iconUnlocked = "", rewardXp = 120 },
            new TokenManager.Achievement { id = "ach7",  title = new TokenManager.LocalizedString { ru = "Дебаггер",          en = "Debugger"           }, iconUnlocked = "", rewardXp = 60  },
            new TokenManager.Achievement { id = "ach8",  title = new TokenManager.LocalizedString { ru = "Ночная сова",       en = "Night Owl"          }, iconUnlocked = "", rewardXp = 30  },
            new TokenManager.Achievement { id = "ach9",  title = new TokenManager.LocalizedString { ru = "Командный игрок",   en = "Team Player"        }, iconUnlocked = "", rewardXp = 90  },
            new TokenManager.Achievement { id = "ach10", title = new TokenManager.LocalizedString { ru = "Легенда",           en = "Legend"             }, iconUnlocked = "", rewardXp = 200 },
        }
    };

    var testUserAch = new TokenManager.UserAchievementListResponse
    {
        data = new[]
        {
            new TokenManager.UserAchievement { id = "ach1",  isPinned = true, pinOrder = 0,  unlockedAt = 1000 },
            new TokenManager.UserAchievement { id = "ach2",  isPinned = true, pinOrder = 1,  unlockedAt = 1001 },
            new TokenManager.UserAchievement { id = "ach3",  isPinned = true, pinOrder = 2,  unlockedAt = 1002 },
            new TokenManager.UserAchievement { id = "ach4",  isPinned = true, pinOrder = 3,  unlockedAt = 1003 },
            new TokenManager.UserAchievement { id = "ach5",  isPinned = true, pinOrder = 4,  unlockedAt = 1004 },
            new TokenManager.UserAchievement { id = "ach6",  isPinned = true, pinOrder = 5,  unlockedAt = 1005 },
            new TokenManager.UserAchievement { id = "ach7",  isPinned = true, pinOrder = 6,  unlockedAt = 1006 },
            new TokenManager.UserAchievement { id = "ach8",  isPinned = true, pinOrder = 7,  unlockedAt = 1007 },
            new TokenManager.UserAchievement { id = "ach9",  isPinned = true, pinOrder = 8,  unlockedAt = 1008 },
            new TokenManager.UserAchievement { id = "ach10", isPinned = true, pinOrder = 9,  unlockedAt = 1009 },
        }
    };

    // Убираем ограничение на 3 ачивки для теста
    int savedMax = maxPinnedAchievements;
    maxPinnedAchievements = 10;
    Apply(testAch, testUserAch);
    maxPinnedAchievements = savedMax;
}

#endif
}
