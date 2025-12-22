using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryPanelUI : MonoBehaviour
{
    [Header("Stats Texts")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private TMP_Text xpText;

    [Header("Buttons")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button nextLevelButton; // если есть

    [Header("Scene Navigation")]
    [SerializeField] private string levelSelectSceneName = "LevelSelectScene";

    private void Awake()
    {
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenu);
            
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevel);
    }

    private void OnEnable()
    {
        Debug.Log("[VictoryPanelUI] Панель активирована");
        
        // Проверяем, что все элементы назначены
        if (timeText == null) Debug.LogError("[VictoryPanelUI] timeText не назначен!");
        if (attemptsText == null) Debug.LogError("[VictoryPanelUI] attemptsText не назначен!");
        if (starsText == null) Debug.LogError("[VictoryPanelUI] starsText не назначен!");
        if (xpText == null) Debug.LogError("[VictoryPanelUI] xpText не назначен!");
    }

    public void SetStats(int timeSeconds, int attempts, int stars, int xp)
    {
        Debug.Log($"[VictoryPanelUI] SetStats вызван: time={timeSeconds}s, attempts={attempts}, stars={stars}, xp={xp}");
        
        if (timeText != null)
        {
            timeText.text = $"Время: {timeSeconds} с";
            Debug.Log($"[VictoryPanelUI] timeText установлен: {timeText.text}");
        }
        
        if (attemptsText != null)
        {
            attemptsText.text = $"Попыток: {attempts}";
            Debug.Log($"[VictoryPanelUI] attemptsText установлен: {attemptsText.text}");
        }
        
        if (starsText != null)
        {
            starsText.text = $"Звезд: {stars}/3";
            Debug.Log($"[VictoryPanelUI] starsText установлен: {starsText.text}");
        }
        
        if (xpText != null)
        {
            xpText.text = xp > 0 ? $"+{xp} XP" : "Загрузка XP...";
            Debug.Log($"[VictoryPanelUI] xpText установлен: {xpText.text}");
        }
    }

    private void OnBackToMenu()
    {
        Debug.Log("[VictoryPanelUI] Возврат в меню выбора уровней");
        
        Time.timeScale = 1f; // сбрасываем паузу, если была
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void OnNextLevel()
    {
        Debug.Log("[VictoryPanelUI] Загрузка следующего уровня");
        
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnNextLevel();
        }
        
        gameObject.SetActive(false);
    }
}
