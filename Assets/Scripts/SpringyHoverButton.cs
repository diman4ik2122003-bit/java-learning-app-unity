using UnityEngine;
using UnityEngine.EventSystems;

public class SpringyHoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float targetScale = 1.12f; // во сколько увеличить кнопку
    [SerializeField] private float springPower = 0.07f; // амплитуда пружины
    [SerializeField] private float springSpeed = 12f;   // скорость пружины

    private Vector3 originalScale;
    private bool isHovered = false;
    private float springTimer = 0f;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isHovered)
        {
            springTimer += Time.unscaledDeltaTime * springSpeed;
            float spring = Mathf.Sin(springTimer * Mathf.PI) * springPower * (1f - Mathf.Clamp01(springTimer / 1.2f));
            float scale = targetScale + spring;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * scale, Time.unscaledDeltaTime * springSpeed);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.unscaledDeltaTime * springSpeed);
            springTimer = 0f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        springTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}
