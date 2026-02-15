using UnityEngine;

/// <summary>
/// Drop this on any GameObject in the scene to auto-fill all AchievementItemView
/// instances with mock data so you can test pin/unpin without a real login.
/// DELETE THIS BEFORE SHIPPING.
/// </summary>
public class AchievementTestFiller : MonoBehaviour
{
    private void Start()
    {
        Debug.LogWarning("[TestFiller] ===== START =====");

        var items = FindObjectsOfType<AchievementItemView>(true);
        Debug.LogWarning($"[TestFiller] Found {items.Length} AchievementItemView(s)");

        for (int i = 0; i < items.Length; i++)
        {
            bool isPinned = (i == 0); // first one starts pinned
            bool canPin = !isPinned;  // others can be pinned

            Debug.LogWarning($"[TestFiller] Binding item {i}: {items[i].gameObject.name}, pinned={isPinned}");

            items[i].Bind(
                achievementId: $"test-ach-{i}",
                title: $"Test Achievement {i + 1}",
                description: "Click to pin/unpin",
                imageUrl: "",
                unlocked: true,
                isPinned: isPinned,
                canPin: canPin
            );
        }

        Debug.LogWarning("[TestFiller] ===== DONE =====");
    }
}
