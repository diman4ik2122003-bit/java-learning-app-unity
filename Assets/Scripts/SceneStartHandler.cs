using UnityEngine;

public class SceneStartHandler : MonoBehaviour
{
    void Start()
    {
        CloudTransitionManager transitionManager = FindFirstObjectByType<CloudTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.EndTransition();
        }
    }
}
