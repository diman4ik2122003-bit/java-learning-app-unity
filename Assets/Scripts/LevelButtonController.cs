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

        // Настраиваем имя целевой сцены для CloudTransitionActivator
        var transitionActivator = GetComponent<CloudTransitionActivator>();
        if (transitionActivator != null)
        {
            string targetScene = GetSceneNameForLevel(data);
            transitionActivator.targetSceneName = targetScene;
            Debug.Log($"[LevelButtonController] Dynamically configured CloudTransitionActivator to load scene: {targetScene}");
        }
    }

    /// <summary>
    /// Метод, вызываемый при нажатии на UI Button (настроен в Unity OnClick событии)
    /// </summary>
    public void OnButtonClick()
    {
        if (levelData != null)
        {
            LevelSelectionManager.SelectedLevel = levelData;
            Debug.Log($"[LevelButtonController] OnButtonClick: SelectedLevel saved as {levelData.levelId}");
        }
        else
        {
            Debug.LogWarning("[LevelButtonController] OnButtonClick: levelData is null!");
        }
    }

    /// <summary>
    /// Возвращает имя сцены, которую необходимо загрузить для данного уровня
    /// </summary>
    private string GetSceneNameForLevel(LevelData data)
    {
        if (data == null) return "Main Menu";

        // 1. Если имя сцены задано вручную в ассете, используем его
        if (!string.IsNullOrEmpty(data.sceneName))
        {
            return data.sceneName;
        }

        string assetName = data.name;

        // 2. Для ассетов вида LevelData2-1, LevelData2-2, LevelData4-1 и т.д.
        if (assetName.StartsWith("LevelData"))
        {
            // Например: "LevelData2-1" -> "Level2_1"
            string suffix = assetName.Substring("LevelData".Length); // "2-1"
            return "Level" + suffix.Replace('-', '_'); // "Level2_1"
        }

        // 3. Для всех остальных ассетов (например, Level1_1, Level1_2, Level1_3 и т.д.)
        //    используем само имя ассета в качестве имени сцены.
        return assetName;
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
