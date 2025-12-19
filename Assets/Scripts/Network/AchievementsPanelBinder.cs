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
    [SerializeField] private Transform verticalContent; // VerticalScrollView/Viewport/Content

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

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

        // Clear UI
        for (int i = verticalContent.childCount - 1; i >= 0; i--)
            Destroy(verticalContent.GetChild(i).gameObject);

        var categories = categoriesResp?.data ?? Array.Empty<TokenManager.AchievementCategory>();
        var all = achievementsResp?.data ?? Array.Empty<TokenManager.Achievement>();
        var mine = mineResp?.data ?? Array.Empty<TokenManager.UserAchievement>();

        var unlockedIds = new HashSet<string>(mine.Select(x => x.id));

        var byCategory = all
            .GroupBy(a => string.IsNullOrEmpty(a.categoryId) ? "__no_category__" : a.categoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var cat in categories.OrderBy(c => c.order))
        {
            var section = CreateSection(verticalContent, cat.name);

            Debug.Log("VERT parent: " + verticalContent.name + " children=" + verticalContent.childCount);
Debug.Log("Section parent path: " + GetPath(section.transform));

            if (!byCategory.TryGetValue(cat.id, out var items))
                items = new List<TokenManager.Achievement>();

            foreach (var ach in items.OrderBy(a => a.title))
            {
                var view = CreateItem(section.ItemsParent);
                bool unlocked = unlockedIds.Contains(ach.id);

                view.Bind(
                    title: ach.title,
                    description: ach.description,
                    unlocked: unlocked
                );
            }
        }

        if (byCategory.TryGetValue("__no_category__", out var noCat) && noCat.Count > 0)
        {
            var section = CreateSection(verticalContent, "Без категории");

            foreach (var ach in noCat.OrderBy(a => a.title))
            {
                var view = CreateItem(section.ItemsParent);
                bool unlocked = unlockedIds.Contains(ach.id);

                view.Bind(
                    title: ach.title,
                    description: ach.description,
                    unlocked: unlocked
                );
            }
        }

        // Force layout to update now (helps in runtime/WebGL)
        Canvas.ForceUpdateCanvases(); // [web:745]
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalContent as RectTransform);

        if (debugLogs)
            Debug.Log($"[AchievementsPanelBinder] Build done. Sections={verticalContent.childCount}", this);
    }

    private CategorySectionView CreateSection(Transform parent, string title)
    {
        var section = Instantiate(categorySectionPrefab);

        // IMPORTANT for UI: do not keep world position [web:721][web:714]
        section.transform.SetParent(parent, false);

        NormalizeRect(section.transform);

        section.SetTitle(title);

        if (debugLogs) LogRect("[Section]", section.transform);

        return section;
    }

    private AchievementItemView CreateItem(Transform parent)
    {
        var view = Instantiate(achievementItemPrefab);

        // IMPORTANT for UI: do not keep world position [web:721][web:714]
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

    static string GetPath(Transform t)
{
    string s = t.name;
    while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
    return s;
}

}
