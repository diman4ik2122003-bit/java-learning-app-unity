using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CloudTransitionManager : MonoBehaviour
{
    public List<TransitionCloud> clouds;
    public float speedMultiplier = 1f; // множитель скорости для всех облаков
    public float sceneLoadDelay = 0.3f;
    public float endTransitionDelay = 0.5f;

    private static CloudTransitionManager instance;
    private int cloudsCompleted = 0;
    private string targetScene;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartTransition(string sceneName)
    {
        if (clouds == null || clouds.Count == 0)
        {
            Debug.LogWarning("Clouds not assigned!");
            return;
        }

        targetScene = sceneName;
        cloudsCompleted = 0;

        foreach (var cloud in clouds)
        {
            cloud.OnMoveComplete -= OnCloudMoveComplete;
            cloud.SetSpeedMultiplier(speedMultiplier); // применяем множитель перед запуском
        }

        foreach (var cloud in clouds)
        {
            cloud.OnMoveComplete += OnCloudMoveComplete;
            cloud.MoveIn();
        }
    }

    private void OnCloudMoveComplete()
    {
        cloudsCompleted++;

        if (cloudsCompleted >= clouds.Count)
        {
            foreach (var cloud in clouds)
            {
                cloud.OnMoveComplete -= OnCloudMoveComplete;
            }
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(targetScene);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedEndTransition());
    }

    private IEnumerator DelayedEndTransition()
    {
        yield return new WaitForSeconds(endTransitionDelay);
        EndTransition();
    }

    public void EndTransition()
    {
        if (clouds == null || clouds.Count == 0)
            return;

        foreach (var cloud in clouds)
        {
            cloud.OnMoveComplete -= OnCloudMoveComplete;
            cloud.SetSpeedMultiplier(speedMultiplier); // применяем множитель перед обратным движением
        }
        foreach (var cloud in clouds)
        {
            cloud.MoveOut();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
