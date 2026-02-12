using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight widget that displays up to 3 pinned achievements (e.g. on Stats tab, Profile, or main menu).
/// Assign an AchievementItemView prefab and a container Transform in the Inspector.
/// Call Refresh() after data is loaded, or it will auto-refresh when enabled.
/// </summary>
public class PinnedAchievementsDisplay : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private AchievementItemView itemPrefab;

    [Header("Container")]
    [SerializeField] private Transform container;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyLabel;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>Rebuild the pinned achievements display from cached TokenManager data.</summary>
    public void Refresh()
    {
        if (!container || !itemPrefab) return;

        // Clear old items
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        var tm = TokenManager.Instance;
        if (tm == null || tm.achievementsMine == null || tm.achievementsAll == null)
        {
            if (emptyLabel) emptyLabel.SetActive(true);
            return;
        }

        var mine = tm.achievementsMine.data;
        var allAchs = tm.achievementsAll.data;

        if (mine == null || allAchs == null)
        {
            if (emptyLabel) emptyLabel.SetActive(true);
            return;
        }

        string lang = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLang
            : "ru";

        // Get pinned, sorted by pinOrder
        var pinned = mine
            .Where(ua => ua.isPinned)
            .OrderBy(ua => ua.pinOrder)
            .ToList();

        if (emptyLabel) emptyLabel.SetActive(pinned.Count == 0);

        var allMap = allAchs.ToDictionary(a => a.id, a => a);

        foreach (var ua in pinned)
        {
            if (!allMap.TryGetValue(ua.id, out var achDef)) continue;

            var view = Instantiate(itemPrefab, container, false);
            view.transform.localScale = Vector3.one;

            string title = achDef.title?.GetText(lang) ?? "";
            string desc = achDef.description?.GetText(lang) ?? "";
            string icon = achDef.iconUnlocked;

            view.Bind(
                achievementId: achDef.id,
                title: title,
                description: desc,
                imageUrl: icon,
                unlocked: true,
                isPinned: true,
                canPin: true
            );

            if (debugLogs)
                Debug.Log($"[PinnedDisplay] Showing pinned: id={achDef.id}, title={title}");
        }

        // Force layout rebuild
        if (container is RectTransform rt)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
