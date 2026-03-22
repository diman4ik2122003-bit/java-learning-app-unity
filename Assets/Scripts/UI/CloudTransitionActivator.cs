using UnityEngine;

public class CloudTransitionActivator : MonoBehaviour
{
    public string targetSceneName; // Имя сцены для загрузки
    private CloudTransitionManager transitionManager;

    void Start()
    {
        transitionManager = FindFirstObjectByType<CloudTransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogError("CloudTransitionManager not found in scene.");
        }
    }

    // Этот метод можно вызвать из UI Button onClick
public void ActivateTransition()
{
    // 1. Обновляем данные
    if (TokenManager.Instance != null)
    {
        TokenManager.Instance.RefreshAll();
    }

    // 2. Стартуем облака
    if (transitionManager != null)
    {
        transitionManager.StartTransition(targetSceneName);
    }
}

}
