using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;
    
    public TMP_Text tooltipText;
    public GameObject tooltipPanel;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        Hide();
    }

    public void Show(string text, Vector2 position)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = text;
            tooltipPanel.SetActive(true);
            
            // позиционируем возле курсора (опционально)
            // tooltipPanel.GetComponent<RectTransform>().position = position;
        }
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
