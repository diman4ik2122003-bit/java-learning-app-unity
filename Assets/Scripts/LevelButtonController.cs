using UnityEngine;
using UnityEngine.UI;

public class LevelButtonController : MonoBehaviour
{
    public LevelData levelData;

    [Header("Stars")]
    public Image star1;
    public Image star2;
    public Image star3;
    public Sprite starEmpty;
    public Sprite starFull;

    [Header("Lock")]
    public GameObject lockOverlay;
    public Button button;

    public void Initialize(LevelData data)
    {
        levelData = data;
        Debug.Log($"[LevelButtonController] Initialize: levelId={levelData?.levelId}");
    }

    public void SetLocked(bool locked)
    {
        Debug.Log($"[LevelButtonController] SetLocked: levelId={levelData?.levelId}, locked={locked}");

        if (button != null)
            button.interactable = !locked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);
    }

    public void SetStars(int stars)
    {
        Debug.Log($"[LevelButtonController] SetStars: levelId={levelData?.levelId}, stars={stars}");

        stars = Mathf.Clamp(stars, 0, 3);
        SetStarImage(star1, stars >= 1);
        SetStarImage(star2, stars >= 2);
        SetStarImage(star3, stars >= 3);
    }

    private void SetStarImage(Image img, bool full)
    {
        if (img == null) return;
        img.sprite = full ? starFull : starEmpty;
    }
}
