using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class IslandSceneManager : MonoBehaviour
{
    public IslandData[] allIslands;     // Все IslandData
    public Image background;            // UI Image для BG
    public Transform levelsParent;      // Родитель для кнопок уровней
    public GameObject levelButtonPrefab;// Префаб UI-кнопки уровня

    public void SetIsland(IslandData data)
    {
        if (data == null) return;

        if (background)
            background.sprite = data.background;

        Debug.Log($"[IslandSceneManager] SetIsland: islandId={data.islandId}");
        RenderLevelButtons(data);
        Debug.Log("[IslandSceneManager] BG CHANGED: " + data.islandId);
    }

    private void RenderLevelButtons(IslandData currentIsland)
    {
        foreach (Transform t in levelsParent)
            Destroy(t.gameObject);

        if (currentIsland.levels == null)
        {
            Debug.LogWarning($"[IslandSceneManager] No levels in island {currentIsland.islandId}");
            return;
        }

        Debug.Log($"[IslandSceneManager] RenderLevelButtons: islandId={currentIsland.islandId}, levels={currentIsland.levels.Length}");

        for (int i = 0; i < currentIsland.levels.Length; i++)
        {
            var level     = currentIsland.levels[i];
            var levelData = level.levelData;

            // ---- создаём кнопку ----
            var btnGO = Instantiate(levelButtonPrefab, levelsParent);
            btnGO.name = $"LevelButton_{(levelData != null ? levelData.levelId : "null")}_{i}";

            var btnRect = btnGO.GetComponent<RectTransform>();
            if (btnRect)
            {
                btnRect.anchoredPosition = level.anchoredPosition;

                if (currentIsland.levelButtonSize != Vector2.zero)
                    btnRect.sizeDelta = currentIsland.levelButtonSize;
            }

            var img = btnGO.GetComponent<Image>();
            if (img)
            {
                img.sprite = level.sprite;
                img.preserveAspect = true;
            }

            // ---- прогресс ----
            string levelId = levelData != null ? levelData.levelId : "<null LevelData>";

            ChallengeProgressDto progress = null;
            bool hasProgress =
                LevelProgressManager.Instance != null &&
                LevelProgressManager.Instance.TryGet(levelId, out progress);

            int  stars     = hasProgress ? progress.stars : 0;
            bool completed = hasProgress && progress.completed;

            Debug.Log($"[IslandSceneManager] Level idx={i}, levelId={levelId}, hasProgress={hasProgress}, completed={completed}, stars={stars}");

            // правило открытия
            bool isLocked = false;
            if (i == 0)
            {
                isLocked = false;
            }
            else
            {
                var prevLevelData = currentIsland.levels[i - 1].levelData;
                string prevLevelId = prevLevelData != null ? prevLevelData.levelId : "<null prev LevelData>";

                bool prevCompleted =
                    LevelProgressManager.Instance != null &&
                    LevelProgressManager.Instance.IsCompleted(prevLevelId);

                isLocked = !prevCompleted;

                Debug.Log($"[IslandSceneManager]   Prev idx={i - 1}, prevLevelId={prevLevelId}, prevCompleted={prevCompleted} => isLocked={isLocked}");
            }

            // ---- контроллер кнопки ----
            var buttonController = btnGO.GetComponent<LevelButtonController>();
            if (buttonController)
            {
                buttonController.Initialize(levelData);
                buttonController.SetLocked(isLocked);
                buttonController.SetStars(stars);

                Debug.Log($"[IslandSceneManager]   Button {btnGO.name}: locked={isLocked}, stars={stars}");
            }
            else
            {
                Debug.LogWarning("[IslandSceneManager] LevelButtonController не найден на префабе кнопки!");
            }
        }
    }

    private void Start()
    {
        string selectedId = PlayerPrefs.GetString("SelectedIsland", "");
        Debug.Log("[IslandSceneManager] Read SelectedIsland = " + selectedId);

        var data = allIslands.FirstOrDefault(i => i.islandId == selectedId);
        if (data != null)
        {
            SetIsland(data);
        }
        else
        {
            Debug.LogWarning("[IslandSceneManager] Selected island not found!");
        }
    }

    public void RefreshCurrentIsland()
{
    string selectedId = PlayerPrefs.GetString("SelectedIsland", "");
    var data = allIslands.FirstOrDefault(i => i.islandId == selectedId);
    if (data != null)
        SetIsland(data);
}
}
