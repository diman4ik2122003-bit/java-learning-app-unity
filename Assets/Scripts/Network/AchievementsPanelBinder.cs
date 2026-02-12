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

        if (debugLogs)
            Debug.Log($"[AchievementsPanelBinder] Categories: {categories.Length}, Achievements: {all.Length}, My: {mine.Length}, Lang: {currentLang}");

        // Build lookup maps
        var unlockedIds = new HashSet<string>(mine.Select(x => x.id));
        var mineMap = mine.ToDictionary(x => x.id, x => x);

        int pinnedCount = mine.Count(x => x.isPinned);

        // ---- Pinned section (optional separate container) ----
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
                string title = achDef.title?.GetText(currentLang) ?? "Unknown";
                string description = achDef.description?.GetText(currentLang) ?? "";
                string imageUrl = achDef.iconUnlocked;

                view.Bind(
                    achievementId: achDef.id,
                    title: title,
                    description: description,
                    imageUrl: imageUrl,
                    unlocked: true,
                    isPinned: true,
                    canPin: true  // already pinned, so unpin is always allowed
                );
            }

            pinnedContainer.gameObject.SetActive(pinnedAchs.Count > 0);
        }

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
        var view = Instantiate(achievementItemPrefab);
        view.transform.SetParent(parent, false);
        NormalizeRect(view.transform);

        if (debugLogs) LogRect("[Item]", view.transform);

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
}
