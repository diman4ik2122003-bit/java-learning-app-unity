using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PinnedAchievementsPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject achievementRowPrefab;
    [SerializeField] private GameObject achievementItemPrefab;

    [Header("Root")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform pinnedContainer;

    [Header("Empty State (optional)")]
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Settings")]
    [SerializeField] private int maxPinnedAchievements = 3;
    [SerializeField] private int achievementsPerRow = 4;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    
    [Header("Loading")]
    [SerializeField] private PanelLoadingController loadingController;  // ← новое

    private int _lastApplyFrame = -1;

    private TokenManager.UserAchievement[] allUserAchievements;
    private TokenManager.Achievement[]     allAchievements;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (pinnedContainer == null && scrollRect != null)
            pinnedContainer = scrollRect.content;

        ShowEmptyState(false); // ← всегда скрыт до первого Apply()
    }

    public void Apply(
        TokenManager.AchievementListResponse     achievementsResp,
        TokenManager.UserAchievementListResponse userAchievementsResp)
    {
        if (debugLogs) Debug.Log("[PinnedAchievementsPanelBinder] Apply called");
        
        if (_lastApplyFrame == Time.frameCount)
        {
            if (debugLogs) Debug.Log("[PinnedAchievementsPanelBinder] Debounce: already applied this frame");
            return;
        }
        _lastApplyFrame = Time.frameCount;

        // ← Сразу сбрасываем empty state, независимо от того, что придёт дальше
        ShowEmptyState(false);

        if (!pinnedContainer || !achievementRowPrefab || !achievementItemPrefab)
        {
            Debug.LogError("[PinnedAchievementsPanelBinder] References not set. " +
                           $"Container:{pinnedContainer != null} " +
                           $"RowPrefab:{achievementRowPrefab != null} " +
                           $"ItemPrefab:{achievementItemPrefab != null}");
            return;
        }

        if (achievementsResp?.data == null || userAchievementsResp?.data == null)
        {
            if (debugLogs) Debug.LogWarning("[PinnedAchievementsPanelBinder] No achievements data");
            ClearContent();
            ShowEmptyState(true);
            loadingController?.StopLoading();
            return;
        }

        allAchievements     = achievementsResp.data;
        allUserAchievements = userAchievementsResp.data;

        var pinned = allUserAchievements
            .Where(ua => ua.isPinned)
            .OrderBy(ua => ua.pinOrder)
            .Take(maxPinnedAchievements)
            .ToList();

        if (debugLogs) Debug.Log($"[PinnedAchievementsPanelBinder] Found {pinned.Count} pinned");

        ClearContent();

        if (pinned.Count == 0)
        {
            ShowEmptyState(true);
            loadingController?.StopLoading(); 
            return;
        }

        //ShowEmptyState(false);

        for (int i = 0; i < pinned.Count; i += achievementsPerRow)
            CreateRow(pinned.Skip(i).Take(achievementsPerRow).ToList());

        Canvas.ForceUpdateCanvases();
        if (pinnedContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        if (debugLogs) Debug.Log("[PinnedAchievementsPanelBinder] Done");
        loadingController?.StopLoading();
    }

    private void CreateRow(List<TokenManager.UserAchievement> userAchievements)
    {
        // *** ИСПРАВЛЕНО: защита от null allAchievements ***
        if (allAchievements == null)
        {
            Debug.LogError("[PinnedAchievementsPanelBinder] allAchievements is null in CreateRow!");
            return;
        }

        GameObject rowObj = Instantiate(achievementRowPrefab, pinnedContainer, false);

        var rt = rowObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        }

        foreach (var userAch in userAchievements)
        {
            var achDef = allAchievements.FirstOrDefault(a => a.id == userAch.id);
            if (achDef == null)
            {
                if (debugLogs) Debug.LogWarning($"[PinnedAchievementsPanelBinder] Def not found: {userAch.id}");
                continue;
            }
            CreateItem(rowObj.transform, achDef, userAch);
        }

        if (debugLogs) Debug.Log($"[PinnedAchievementsPanelBinder] Row created with {userAchievements.Count} items");
    }

    private void CreateItem(Transform parent, TokenManager.Achievement achievement, TokenManager.UserAchievement userAchievement)
    {
        GameObject itemObj = Instantiate(achievementItemPrefab, parent, false);

        var view = itemObj.GetComponent<AchievementItemPinnedView>();
        if (view != null)
        {
            view.Bind(achievement, userAchievement);
        }
        else
        {
            var titleText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                string lang = LocalizationManager.Instance?.CurrentLang ?? "ru";
                titleText.text = achievement.title?.GetText(lang) ?? "";
            }
            if (debugLogs) Debug.LogWarning("[PinnedAchievementsPanelBinder] AchievementItemPinnedView not found, used fallback");
        }
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
            else
            {
                // *** ИСПРАВЛЕНО: сразу скрываем, чтобы Apply не видел старые объекты в тот же кадр ***
                child.SetActive(false);
                Destroy(child);
            }
#else
            child.SetActive(false);
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
                new TokenManager.Achievement { id = "ach11", title = new TokenManager.LocalizedString { ru = "Одиночка",          en = "Solo"               }, iconUnlocked = "", rewardXp = 40  },
                new TokenManager.Achievement { id = "ach12", title = new TokenManager.LocalizedString { ru = "Финишёр",           en = "Finisher"           }, iconUnlocked = "", rewardXp = 150 },
                new TokenManager.Achievement { id = "ach13", title = new TokenManager.LocalizedString { ru = "Исследователь",     en = "Explorer"           }, iconUnlocked = "", rewardXp = 80  },
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
                new TokenManager.UserAchievement { id = "ach11", isPinned = true, pinOrder = 10, unlockedAt = 1010 },
                new TokenManager.UserAchievement { id = "ach12", isPinned = true, pinOrder = 11, unlockedAt = 1011 },
                new TokenManager.UserAchievement { id = "ach13", isPinned = true, pinOrder = 12, unlockedAt = 1012 },
            }
        };

        int savedMax    = maxPinnedAchievements;
        int savedPerRow = achievementsPerRow;
        maxPinnedAchievements = 13;
        achievementsPerRow    = 4;
        Apply(testAch, testUserAch);
        maxPinnedAchievements = savedMax;
        achievementsPerRow    = savedPerRow;
    }
#endif
}
