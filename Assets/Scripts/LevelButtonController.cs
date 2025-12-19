// Assets/Scripts/LevelScripts/LevelButtonController.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButtonController : MonoBehaviour
{
    private LevelData assignedLevelData;

    // Вызывается из IslandSceneManager при создании кнопки
    public void Initialize(LevelData levelData)
    {
        assignedLevelData = levelData;

        var button = GetComponent<Button>();
        if (!levelData)
        {
            Debug.LogWarning("[LevelButtonController] LevelData not assigned!", this);
            if (button) button.interactable = false;
        }
    }

    // Вызывается из Button.onClick в инспекторе (ДО CloudTransition)
    public void OnButtonClick()
    {
        if (!assignedLevelData)
        {
            Debug.LogError("[LevelButtonController] LevelData is null!", this);
            return;
        }

        // ⭐ Сохраняем выбранный уровень в статическое хранилище
        LevelSelectionManager.SelectedLevel = assignedLevelData;
        
        Debug.Log("[LevelButtonController] Selected level: " + assignedLevelData.levelId);
        
        // CloudTransition запустится следующим событием в Button.onClick
    }
}
