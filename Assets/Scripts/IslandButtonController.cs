using UnityEngine;
using UnityEngine.UI;

public class IslandButtonController : MonoBehaviour
{
    public string islandId;
    public GameObject lockOverlay;
    public Button button;

    private void Start()
    {
        RefreshLock();
    }

    public void RefreshLock()
    {
        bool unlocked =
            IslandsProgressManager.Instance == null ||
            IslandsProgressManager.Instance.IsUnlocked(islandId);

        if (button != null)
            button.interactable = unlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        Debug.Log($"[IslandButtonController] islandId={islandId}, unlocked={unlocked}");
    }
}
