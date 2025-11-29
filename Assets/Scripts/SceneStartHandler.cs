using UnityEngine;

public class SceneStartHandler : MonoBehaviour
{
    void Start()
    {
        CloudTransitionManager transitionManager = FindObjectOfType<CloudTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.EndTransition();
        }
    }
}
