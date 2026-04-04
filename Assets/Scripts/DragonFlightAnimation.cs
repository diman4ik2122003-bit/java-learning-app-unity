using UnityEngine;
using UnityEngine.UI;

public class DragonFlightUI : MonoBehaviour
{
    [Header("Настройки кадров")]
    public Sprite[] sprites;
    public float frameRate = 0.1f;

    [Header("Настройки движения")]
    public float stepUp = 10.0f; // Величина прыжка в пикселях

    private Image uiImage;
    private RectTransform rectTransform;
    
    private int currentFrame = 0;
    private int direction = 1;
    private float timer;
    private Vector3 startLocalPos;

    void Start()
    {
        uiImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        // Запоминаем точку старта, чтобы не улететь в бесконечность
        startLocalPos = rectTransform.localPosition;

        if (uiImage == null) Debug.LogError("Добавь Image на этот объект!");
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            UpdateAnimation();
        }
    }

    void UpdateAnimation()
    {
        // 1. Смена кадра
        currentFrame += direction;

        if (currentFrame >= sprites.Length - 1)
        {
            currentFrame = sprites.Length - 1;
            direction = -1;
        }
        else if (currentFrame <= 0)
        {
            currentFrame = 0;
            direction = 1;
        }

        // 2. Меняем спрайт
        uiImage.sprite = sprites[currentFrame];

        // 3. Двигаем объект (localPosition гарантирует движение без растяжения)
        // Считаем смещение относительно начальной точки
        float offset = currentFrame * stepUp; 
        rectTransform.localPosition = startLocalPos + new Vector3(0, offset, 0);
    }
}