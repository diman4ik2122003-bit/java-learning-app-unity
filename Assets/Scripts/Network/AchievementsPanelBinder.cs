using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsPanelBinder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private CategorySectionView categorySectionPrefab;
    [SerializeField] private AchievementItemView achievementItemPrefab;

    [Header("Root")]
    [SerializeField] private Transform verticalContent;

    [Header("Pinned Section (optional)")]
    [SerializeField] private Transform pinnedContainer;

    [Header("Loading")]
    [SerializeField] private PanelLoadingController loadingController;  // ← новое

    private TokenManager.AchievementCategoryListResponse _pendingCategories;
    private TokenManager.AchievementListResponse _pendingAchievements;
    private TokenManager.UserAchievementListResponse _pendingMine;
    private bool _hasPendingData = false;

    private void OnEnable()
    {
        if (_hasPendingData)
            RebuildUI();
    }

    public void Apply(
        TokenManager.AchievementCategoryListResponse categoriesResp,
        TokenManager.AchievementListResponse achievementsResp,
        TokenManager.UserAchievementListResponse mineResp)
    {
        _pendingCategories   = categoriesResp;
        _pendingAchievements = achievementsResp;
        _pendingMine         = mineResp;
        _hasPendingData      = true;

        loadingController?.StopLoading();
        if (!gameObject.activeInHierarchy) return;

        RebuildUI();
    }

    private void RebuildUI()
    {
        _hasPendingData = false;

        if (!verticalContent || !categorySectionPrefab || !achievementItemPrefab)
        {
            Debug.LogError("[AchievementsPanelBinder] References not set.");
            return;
        }

        string currentLang = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLang
            : "ru";

        for (int i = verticalContent.childCount - 1; i >= 0; i--)
            Destroy(verticalContent.GetChild(i).gameObject);

        var categories = _pendingCategories?.data  ?? Array.Empty<TokenManager.AchievementCategory>();
        var all        = _pendingAchievements?.data ?? Array.Empty<TokenManager.Achievement>();
        var mine       = _pendingMine?.data         ?? Array.Empty<TokenManager.UserAchievement>();

        var unlockedIds = new HashSet<string>(mine.Select(x => x.id));
        var mineMap     = mine.ToDictionary(x => x.id, x => x);
        int pinnedCount = mine.Count(x => x.isPinned);

        // ── Pinned section ──────────────────────────────────────────────
        if (pinnedContainer)
        {
            for (int i = pinnedContainer.childCount - 1; i >= 0; i--)
                Destroy(pinnedContainer.GetChild(i).gameObject);

            var pinnedAchs = mine
                .Where(ua => ua.isPinned)
                .OrderBy(ua => ua.pinOrder)
                .ToList();

            foreach (var ua in pinnedAchs)
            {
                var achDef = all.FirstOrDefault(a => a.id == ua.id);
                if (achDef == null) continue;

                var view = CreateItem(pinnedContainer);
                view.Bind(
                    achievementId: achDef.id,
                    title:         achDef.title?.GetText(currentLang) ?? "Unknown",
                    description:   achDef.description?.GetText(currentLang) ?? "",
                    imageUrl:      achDef.iconUnlocked,
                    unlocked:      true,
                    isPinned:      true,
                    canPin:        true
                );
            }

            pinnedContainer.gameObject.SetActive(pinnedAchs.Count > 0);
        }
        else if (pinnedCount > 0)
        {
            string pinnedTitle = currentLang == "en" ? "Pinned" : "Закреплённые";
            var pinnedSection = CreateSection(verticalContent, pinnedTitle);

            var pinnedAchs = mine
                .Where(ua => ua.isPinned)
                .OrderBy(ua => ua.pinOrder)
                .ToList();

            foreach (var ua in pinnedAchs)
            {
                var achDef = all.FirstOrDefault(a => a.id == ua.id);
                if (achDef == null) continue;

                var view = CreateItem(pinnedSection.ItemsParent);
                view.Bind(
                    achievementId: achDef.id,
                    title:         achDef.title?.GetText(currentLang) ?? "Unknown",
                    description:   achDef.description?.GetText(currentLang) ?? "",
                    imageUrl:      achDef.iconUnlocked,
                    unlocked:      true,
                    isPinned:      true,
                    canPin:        true
                );
            }

            pinnedSection.OnItemsAdded();
        }

        // ── Categories ──────────────────────────────────────────────────
        var byCategory = all
            .GroupBy(a => string.IsNullOrEmpty(a.categoryId) ? "__no_category__" : a.categoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var cat in categories.OrderBy(c => c.order))
        {
            string categoryTitle = cat.name?.GetText(currentLang) ?? "Категория";
            var section = CreateSection(verticalContent, categoryTitle);

            if (!byCategory.TryGetValue(cat.id, out var items))
                items = new List<TokenManager.Achievement>();

            foreach (var ach in items.OrderBy(a => a.order))
            {
                bool unlocked = unlockedIds.Contains(ach.id);
                bool isPinned = unlocked && mineMap.TryGetValue(ach.id, out var ua) && ua.isPinned;
                bool canPin   = unlocked && !isPinned && pinnedCount < 3;

                var view = CreateItem(section.ItemsParent);
                view.Bind(
                    achievementId: ach.id,
                    title:         ach.title?.GetText(currentLang) ?? "Неизвестно",
                    description:   ach.description?.GetText(currentLang) ?? "",
                    imageUrl:      unlocked ? ach.iconUnlocked : ach.iconLocked,
                    unlocked:      unlocked,
                    isPinned:      isPinned,
                    canPin:        canPin
                );
            }

            section.OnItemsAdded();
        }

        // ── No category ─────────────────────────────────────────────────
        if (byCategory.TryGetValue("__no_category__", out var noCat) && noCat.Count > 0)
        {
            var section = CreateSection(verticalContent, "Без категории");

            foreach (var ach in noCat.OrderBy(a => a.order))
            {
                bool unlocked = unlockedIds.Contains(ach.id);
                bool isPinned = unlocked && mineMap.TryGetValue(ach.id, out var ua) && ua.isPinned;
                bool canPin   = unlocked && !isPinned && pinnedCount < 3;

                var view = CreateItem(section.ItemsParent);
                view.Bind(
                    achievementId: ach.id,
                    title:         ach.title?.GetText(currentLang) ?? "Неизвестно",
                    description:   ach.description?.GetText(currentLang) ?? "",
                    imageUrl:      unlocked ? ach.iconUnlocked : ach.iconLocked,
                    unlocked:      unlocked,
                    isPinned:      isPinned,
                    canPin:        canPin
                );
            }

            section.OnItemsAdded();
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);
    }

    private CategorySectionView CreateSection(Transform parent, string title)
    {
        var section = Instantiate(categorySectionPrefab);
        section.transform.SetParent(parent, false);
        NormalizeRect(section.transform);
        section.SetTitle(title);
        return section;
    }

    private AchievementItemView CreateItem(Transform parent)
    {
        var view = Instantiate(achievementItemPrefab);
        view.transform.SetParent(parent, false);
        NormalizeRect(view.transform);
        return view;
    }

    private static void NormalizeRect(Transform t)
    {
        t.localScale    = Vector3.one;
        t.localRotation = Quaternion.identity;

        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
        else
            t.localPosition = Vector3.zero;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Fill Achievements")]
    private void TestFillAchievements()
    {
        var testCategories = new TokenManager.AchievementCategoryListResponse
        {
            data = new TokenManager.AchievementCategory[]
            {
                new TokenManager.AchievementCategory { id = "cat1", name = new TokenManager.LocalizedString { ru = "Обучение",  en = "Learning"    }, order = 0 },
                new TokenManager.AchievementCategory { id = "cat2", name = new TokenManager.LocalizedString { ru = "Задания",   en = "Challenges"  }, order = 1 }
            }
        };

        var testAchievements = new TokenManager.AchievementListResponse
        {
            data = new TokenManager.Achievement[]
            {
                new TokenManager.Achievement { id="ach1", categoryId="cat1", order=0, title=new TokenManager.LocalizedString{ru="Первые шаги",      en="First Steps"},         description=new TokenManager.LocalizedString{ru="Завершите первый урок",  en="Complete first lesson"},  iconUnlocked="", iconLocked="", rewardXp=10  },
                new TokenManager.Achievement { id="ach2", categoryId="cat2", order=0, title=new TokenManager.LocalizedString{ru="Код-мастер",        en="Code Master"},         description=new TokenManager.LocalizedString{ru="Решите 10 задач",        en="Solve 10 challenges"},    iconUnlocked="", iconLocked="", rewardXp=50  },
                new TokenManager.Achievement { id="ach3", categoryId="cat1", order=1, title=new TokenManager.LocalizedString{ru="Стример знаний",    en="Knowledge Streaker"},  description=new TokenManager.LocalizedString{ru="Учитесь 7 дней подряд",  en="Study 7 days in a row"}, iconUnlocked="", iconLocked="", rewardXp=100 },
                new TokenManager.Achievement { id="ach4", categoryId="",     order=0, title=new TokenManager.LocalizedString{ru="Секрет",            en="Secret"},              description=new TokenManager.LocalizedString{ru="Найдена пасхалка",       en="Easter egg found"},       iconUnlocked="", iconLocked="", rewardXp=500 }
            }
        };

        var testUserAchievements = new TokenManager.UserAchievementListResponse
        {
            data = new TokenManager.UserAchievement[]
            {
                new TokenManager.UserAchievement { id="ach1", isPinned=true,  pinOrder=0, unlockedAt=1234567890 },
                new TokenManager.UserAchievement { id="ach2", isPinned=false, pinOrder=0, unlockedAt=1234567891 }
            }
        };

        Apply(testCategories, testAchievements, testUserAchievements);
    }
#endif
}
