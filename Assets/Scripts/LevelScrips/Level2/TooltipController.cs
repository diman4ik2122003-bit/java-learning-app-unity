using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TooltipController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipText = "Подсказка";
    public static TMP_Text tooltipDisplay;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipDisplay != null)
        {
            tooltipDisplay.text = tooltipText;
            tooltipDisplay.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipDisplay != null)
            tooltipDisplay.gameObject.SetActive(false);
    }
}
