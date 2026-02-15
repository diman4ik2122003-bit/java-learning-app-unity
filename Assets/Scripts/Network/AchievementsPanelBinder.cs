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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public void Apply(
        TokenManager.AchievementCategoryListResponse categoriesResp,
        TokenManager.AchievementListResponse achievementsResp,
        TokenManager.UserAchievementListResponse mineResp)
    {
        Debug.LogError("!!! APPLY CALLED !!!");
        Debug.LogError($"verticalContent={verticalContent != null}, categorySectionPrefab={categorySectionPrefab != null}, achievementItemPrefab={achievementItemPrefab != null}");

        if (!verticalContent || !categorySectionPrefab || !achievementItemPrefab)
        {
            Debug.LogError("[AchievementsPanelBinder] References not set.");
            return;
        }

        string currentLang = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.CurrentLang 
            : "ru";

        // Clear old content
        for (int i = verticalContent.childCount - 1; i >= 0; i--)
            Destroy(verticalContent.GetChild(i).gameObject);

        var categories = categoriesResp?.data ?? Array.Empty<TokenManager.AchievementCategory>();
        var all = achievementsResp?.data ?? Array.Empty<TokenManager.Achievement>();
        var mine = mineResp?.data ?? Array.Empty<TokenManager.UserAchievement>();

        Debug.LogError($"[AchievementsPanelBinder] Categories: {categories.Length}, Achievements: {all.Length}, My: {mine.Length}, Lang: {currentLang}");
        
        if (debugLogs)
            Debug.Log($"[AchievementsPanelBinder] Categories: {categories.Length}, Achievements: {all.Length}, My: {mine.Length}, Lang: {currentLang}");

        // Build lookup maps
        var unlockedIds = new HashSet<string>(mine.Select(x => x.id));
        var mineMap = mine.ToDictionary(x => x.id, x => x);

        int pinnedCount = mine.Count(x => x.isPinned);

        // ---- Category sections ----
        var byCategory = all
            .GroupBy(a => string.IsNullOrEmpty(a.categoryId) ? "__no_category__" : a.categoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var cat in categories.OrderBy(c => c.order))
        {
            string categoryTitle = cat.name?.GetText(currentLang) ?? "Категория";

            if (debugLogs)
                Debug.Log($"[AchievementsPanelBinder] Processing category: id={cat.id}, title={categoryTitle}");

            var section = CreateSection(verticalContent, categoryTitle);

            if (!byCategory.TryGetValue(cat.id, out var items))
            {
                items = new List<TokenManager.Achievement>();
                if (debugLogs)
                    Debug.Log($"[AchievementsPanelBinder] No achievements for category {cat.id}");
            }

            foreach (var ach in items.OrderBy(a => a.order))
            {
                var view = CreateItem(section.ItemsParent);
                bool unlocked = unlockedIds.Contains(ach.id);

                bool isPinned = false;
                if (unlocked && mineMap.TryGetValue(ach.id, out var ua))
                    isPinned = ua.isPinned;

                bool canPin = unlocked && !isPinned && pinnedCount < 3;

                string title = ach.title?.GetText(currentLang) ?? "Неизвестно";
                string description = ach.description?.GetText(currentLang) ?? "";
                string imageUrl = unlocked ? ach.iconUnlocked : ach.iconLocked;

                if (debugLogs)
                    Debug.Log($"[AchievementsPanelBinder] Binding: id={ach.id}, title={title}, unlocked={unlocked}, isPinned={isPinned}, canPin={canPin}");

                view.Bind(
                    achievementId: ach.id,
                    title: title,
                    description: description,
                    imageUrl: imageUrl,
                    unlocked: unlocked,
                    isPinned: isPinned,
                    canPin: canPin
                );
            }
            section.OnItemsAdded();
        }

        // Achievements without category
        if (byCategory.TryGetValue("__no_category__", out var noCat) && noCat.Count > 0)
        {
            var section = CreateSection(verticalContent, "Без категории");

            foreach (var ach in noCat.OrderBy(a => a.order))
            {
                var view = CreateItem(section.ItemsParent);
                bool unlocked = unlockedIds.Contains(ach.id);

                bool isPinned = false;
                if (unlocked && mineMap.TryGetValue(ach.id, out var ua))
                    isPinned = ua.isPinned;

                bool canPin = unlocked && !isPinned && pinnedCount < 3;

                string title = ach.title?.GetText(currentLang) ?? "Неизвестно";
                string description = ach.description?.GetText(currentLang) ?? "";
                string imageUrl = unlocked ? ach.iconUnlocked : ach.iconLocked;

                view.Bind(
                    achievementId: ach.id,
                    title: title,
                    description: description,
                    imageUrl: imageUrl,
                    unlocked: unlocked,
                    isPinned: isPinned,
                    canPin: canPin
                );
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);

        if (debugLogs)
            Debug.Log($"[AchievementsPanelBinder] Build done. Sections={verticalContent.childCount}, Pinned={pinnedCount}", this);
    }

    private CategorySectionView CreateSection(Transform parent, string title)
    {
        var section = Instantiate(categorySectionPrefab);
        section.transform.SetParent(parent, false);
        NormalizeRect(section.transform);
        section.SetTitle(title);

        if (debugLogs) LogRect("[Section]", section.transform);

        return section;
    }

    private AchievementItemView CreateItem(Transform parent)
{
    Debug.LogError($"!!! CreateItem CALLED !!! prefab={achievementItemPrefab != null}");
    
    var view = Instantiate(achievementItemPrefab);
    
    Debug.LogError($"!!! Instantiate DONE !!! view={view != null}, name={view.name}");
    
    view.transform.SetParent(parent, false);
    NormalizeRect(view.transform);

    if (debugLogs) LogRect("[Item]", view.transform);

    Debug.LogError($"!!! CreateItem DONE !!! Calling Bind next...");
    
    return view;
}


    private static void NormalizeRect(Transform t)
    {
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;

        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
        else
            t.localPosition = Vector3.zero;
    }

    private static void LogRect(string tag, Transform t)
    {
        if (t is not RectTransform rt) return;

        Debug.Log($"{tag} name={t.name} parent={t.parent?.name} " +
                  $"anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot} " +
                  $"anchoredPos={rt.anchoredPosition} sizeDelta={rt.sizeDelta}");
    }

    // ========== ТЕСТОВЫЙ МЕТОД ==========
#if UNITY_EDITOR
    [ContextMenu("Test Fill Achievements")]
    private void TestFillAchievements()
    {
        // Создаем тестовые категории
        var testCategories = new TokenManager.AchievementCategoryListResponse
        {
            data = new TokenManager.AchievementCategory[]
            {
                new TokenManager.AchievementCategory
                {
                    id = "learning",
                    name = new TokenManager.LocalizedString 
                    { 
                        ru = "Обучение", 
                        en = "Learning" 
                    },
                    order = 1
                },
                new TokenManager.AchievementCategory
                {
                    id = "challenges",
                    name = new TokenManager.LocalizedString 
                    { 
                        ru = "Испытания", 
                        en = "Challenges" 
                    },
                    order = 2
                },
                new TokenManager.AchievementCategory
                {
                    id = "social",
                    name = new TokenManager.LocalizedString 
                    { 
                        ru = "Социальные", 
                        en = "Social" 
                    },
                    order = 3
                }
            }
        };

        // Создаем тестовые достижения
        var testAchievements = new TokenManager.AchievementListResponse
        {
            data = new TokenManager.Achievement[]
            {
                // Категория: Обучение
                new TokenManager.Achievement
                {
                    id = "first_lesson",
                    categoryId = "learning",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Первый урок", 
                        en = "First Lesson" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Завершите первый урок по Java", 
                        en = "Complete first Java lesson" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 1
                },
                new TokenManager.Achievement
                {
                    id = "10_lessons",
                    categoryId = "learning",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Десятка уроков", 
                        en = "Ten Lessons" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Завершите 10 уроков", 
                        en = "Complete 10 lessons" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 2
                },
                new TokenManager.Achievement
                {
                    id = "first_course",
                    categoryId = "learning",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Первый курс", 
                        en = "First Course" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Завершите первый курс", 
                        en = "Complete first course" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 3
                },
                
                // Категория: Испытания
                new TokenManager.Achievement
                {
                    id = "first_challenge",
                    categoryId = "challenges",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Первое испытание", 
                        en = "First Challenge" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Решите первое испытание", 
                        en = "Solve first challenge" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 1
                },
                new TokenManager.Achievement
                {
                    id = "speed_demon",
                    categoryId = "challenges",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Скоростной демон", 
                        en = "Speed Demon" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Решите испытание за 1 минуту", 
                        en = "Solve challenge in 1 minute" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 2
                },
                
                // Категория: Социальные
                new TokenManager.Achievement
                {
                    id = "first_friend",
                    categoryId = "social",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Первый друг", 
                        en = "First Friend" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Добавьте первого друга", 
                        en = "Add first friend" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 1
                },
                new TokenManager.Achievement
                {
                    id = "popular",
                    categoryId = "social",
                    title = new TokenManager.LocalizedString 
                    { 
                        ru = "Популярный", 
                        en = "Popular" 
                    },
                    description = new TokenManager.LocalizedString 
                    { 
                        ru = "Добавьте 10 друзей", 
                        en = "Add 10 friends" 
                    },
                    iconUnlocked = "",
                    iconLocked = "",
                    order = 2
                }
            }
        };

        // Создаем мои достижения (unlocked + 1 pinned)
        var testMine = new TokenManager.UserAchievementListResponse
        {
            data = new TokenManager.UserAchievement[]
            {
                new TokenManager.UserAchievement
                {
                    id = "first_lesson",
                    isPinned = true,
                    pinOrder = 1
                },
                new TokenManager.UserAchievement
                {
                    id = "10_lessons",
                    isPinned = false
                },
                new TokenManager.UserAchievement
                {
                    id = "first_challenge",
                    isPinned = false
                },
                new TokenManager.UserAchievement
                {
                    id = "first_friend",
                    isPinned = false
                }
            }
        };

        // Вызываем Apply с тестовыми данными
        Apply(testCategories, testAchievements, testMine);

        Debug.Log("[AchievementsPanelBinder] ✅ Test data filled! 3 categories, 7 achievements (4 unlocked, 1 pinned)");
    }
#endif
}
