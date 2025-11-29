// Assets/Scripts/IslandSelector.cs
using UnityEngine;

public class IslandSelector : MonoBehaviour
{
    public IslandData islandData;   // Перетащи сюда нужный IslandData asset

    public void OnClick()
    {
        if (islandData != null)
        {
            PlayerPrefs.SetString("SelectedIsland", islandData.islandId);
            Debug.Log("[SELECTOR] Set SelectedIsland = " + islandData.islandId);
            // Переход на сцену делает твой CloudTransitionActivator,
            // тут просто сохраняем выбор.
        }
    }
}
