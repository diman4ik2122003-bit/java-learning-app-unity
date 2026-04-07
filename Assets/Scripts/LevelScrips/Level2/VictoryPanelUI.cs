using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryPanelUI : MonoBehaviour
{
    [Header("Stats Texts")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text xpText;

    [Header("Stars")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private Sprite starFilled;

    [Header("Buttons")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button nextLevelButton;

    [Header("Scene Navigation")]
    [SerializeField] private string levelSelectSceneName = "LevelSelectScene";
    [SerializeField] private string nextLevelSceneName = "LevelSelectScene";

    private CloudTransitionManager _transitionManager;

    private void Awake()
    {
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenu);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevel);
    }

    private void Start()
    {
        _transitionManager = FindFirstObjectByType<CloudTransitionManager>();
        if (_transitionManager == null)
            Debug.LogWarning("[VictoryPanelUI] CloudTransitionManager не найден в сцене!");
    }

    private void OnEnable()
    {
        if (timeText == null)     Debug.LogError("[VictoryPanelUI] timeText не назначен!");
        if (attemptsText == null) Debug.LogError("[VictoryPanelUI] attemptsText не назначен!");
        if (xpText == null)       Debug.LogError("[VictoryPanelUI] xpText не назначен!");
        if (star1 == null || star2 == null || star3 == null)
            Debug.LogWarning("[VictoryPanelUI] Одна или несколько Image-звёзд не назначены!");
        if (starEmpty == null || starFilled == null)
            Debug.LogWarning("[VictoryPanelUI] Спрайты starEmpty / starFilled не назначены!");
    }

    public void SetStats(int timeSeconds, int attempts, int stars, int xp)
    {
        Debug.Log($"[VictoryPanelUI] SetStats: time={timeSeconds}s, attempts={attempts}, stars={stars}, xp={xp}");

        if (timeText != null)
            timeText.text = $"Время: {timeSeconds} с";

        if (attemptsText != null)
            attemptsText.text = $"Попыток: {attempts}";

        UpdateStars(stars);

        if (xpText != null)
            xpText.text = xp > 0 ? $"+{xp} XP" : "Загрузка XP...";
    }

    // ─── Приватные хелперы ────────────────────────────────────────────────

    private void UpdateStars(int count)
    {
        if (starEmpty == null || starFilled == null) return;

        if (star1 != null) star1.sprite = count >= 1 ? starFilled : starEmpty;
        if (star2 != null) star2.sprite = count >= 2 ? starFilled : starEmpty;
        if (star3 != null) star3.sprite = count >= 3 ? starFilled : starEmpty;
    }

    private void TransitionToScene(string sceneName)
    {
        Time.timeScale = 1f;

        // Пробуем взять менеджер повторно — он DontDestroyOnLoad, мог создаться после Start
        if (_transitionManager == null)
            _transitionManager = FindFirstObjectByType<CloudTransitionManager>();

        if (TokenManager.Instance != null)
            TokenManager.Instance.RefreshAll();

        if (_transitionManager != null)
            _transitionManager.StartTransition(sceneName);
        else
        {
            Debug.LogWarning("[VictoryPanelUI] CloudTransitionManager не найден, переход без облаков");
            SceneManager.LoadScene(sceneName);
        }
    }

    // ─── Обработчики кнопок ───────────────────────────────────────────────

    private void OnBackToMenu()
    {
        Debug.Log("[VictoryPanelUI] → Меню выбора уровней");
        TransitionToScene(levelSelectSceneName);
    }

    private void OnNextLevel()
    {
        Debug.Log("[VictoryPanelUI] → Следующий уровень");
        TransitionToScene(nextLevelSceneName);
    }
}