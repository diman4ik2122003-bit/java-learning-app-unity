using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Во сколько раз увеличить (например 1.2 = 120%)
    [SerializeField] private float scaleMultiplier = 1.15f;
    // Скорость анимации
    [SerializeField] private float scaleSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool hovering;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Плавное увеличение/уменьшение
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        targetScale = originalScale * scaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        targetScale = originalScale;
    }
}
