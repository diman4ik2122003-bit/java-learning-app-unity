using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JellyButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private float jellyPower = 0.2f; // насколько сильно "желе"
    [SerializeField] private float jellyDuration = 0.2f; // длительность эффекта

    private Vector3 originalScale;
    private float timer = 0;
    private bool jellyActive = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        timer = 0;
        jellyActive = true;
    }

    void Update()
    {
        if (jellyActive)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / jellyDuration;
            if (t < 1f)
            {
                float jelly = Mathf.Sin(t * Mathf.PI * 3) * jellyPower * (1.0f - t);
                transform.localScale = originalScale + new Vector3(-jelly, jelly, 0);
            }
            else
            {
                transform.localScale = originalScale;
                jellyActive = false;
            }
        }
    }
}
