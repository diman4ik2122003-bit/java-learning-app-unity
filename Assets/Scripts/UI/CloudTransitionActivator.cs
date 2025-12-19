using UnityEngine;

public class CloudTransitionActivator : MonoBehaviour
{
    public string targetSceneName; // Имя сцены для загрузки
    private CloudTransitionManager transitionManager;

    void Start()
    {
        transitionManager = FindObjectOfType<CloudTransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogError("CloudTransitionManager not found in scene.");
        }
    }

    // Этот метод можно вызвать из UI Button onClick
    public void ActivateTransition()
    {
        if (transitionManager != null)
        {
            transitionManager.StartTransition(targetSceneName);
        }
    }
}
