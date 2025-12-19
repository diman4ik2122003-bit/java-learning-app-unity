// Assets/Scripts/IslandSceneManager.cs
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class IslandSceneManager : MonoBehaviour
{
    public IslandData[] allIslands;        // Все IslandData
    public Image background;               // UI Image для BG
    public Transform levelsParent;         // Родитель для кнопок уровней
    public GameObject levelButtonPrefab;   // Префаб UI-кнопки уровня

    public void SetIsland(IslandData data)
    {
        if (data == null) return;

        if (background)
            background.sprite = data.background;

        RenderLevelButtons(data);
        Debug.Log("[IslandSceneManager] BG CHANGED: " + data.islandId);
    }

    private void RenderLevelButtons(IslandData currentIsland)
    {
        // Удаляем старые кнопки
        foreach (Transform t in levelsParent)
            Destroy(t.gameObject);

        if (currentIsland.levels == null) return;

        // Создаём новые по данным
        for (int i = 0; i < currentIsland.levels.Length; i++)
        {
            var level = currentIsland.levels[i];

            var btnGO = Instantiate(levelButtonPrefab, levelsParent);

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

            // ⭐ Передаём LevelData в кнопку через LevelButtonController
            var buttonController = btnGO.GetComponent<LevelButtonController>();
            if (buttonController)
            {
                buttonController.Initialize(level.levelData);
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
}
