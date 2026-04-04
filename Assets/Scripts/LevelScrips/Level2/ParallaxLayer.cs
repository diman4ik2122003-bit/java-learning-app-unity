using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Настройки параллакса")]
    [Tooltip("Сила параллакса. \n1 = фон едет вместе с камерой (очень далеко).\n0 = стоит на месте, как обычный объект.")]
    public Vector2 parallaxMultiplier = new Vector2(0.5f, 0.5f);

    [Header("Бесконечная прокрутка (по горизонтали)")]
    [Tooltip("Автоматически повторять картинку по бокам?")]
    public bool infiniteLoopX = true;
    
    [Tooltip("Скорость самостоятельного движения по X (для облаков).")]
    public float autoScrollSpeedX = 0f;

    private Transform cam;
    private Vector3 startPos;
    private float lengthX;

    void Start()
    {
        // Ищем GameCamera
        GameObject camObj = GameObject.Find("GameCamera");
        if (camObj != null) 
            cam = camObj.transform;
        else 
            cam = Camera.main?.transform;

        // Корректируем стартовую позицию, чтобы фон не "прыгал" в начале игры.
        // Вычитаем изначальное влияние параллакса, чтобы точка спавна осталась там, где вы её поставили.
        if (cam != null)
        {
            startPos = new Vector3(
                transform.position.x - cam.position.x * parallaxMultiplier.x,
                transform.position.y - cam.position.y * parallaxMultiplier.y,
                transform.position.z
            );
        }
        else
        {
            startPos = transform.position;
        }
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        lengthX = sr.bounds.size.x; // Узнаем реальную ширину картинки в мире

        // Если нужен бесконечный фон, создаём копии справа и слева
        if (infiniteLoopX && lengthX > 0)
        {
            CreateClone(lengthX);   // Правая копия
            CreateClone(-lengthX);  // Левая копия
        }
    }

    private void CreateClone(float offsetX)
    {
        GameObject clone = new GameObject(gameObject.name + "_Clone");
        clone.transform.SetParent(transform);
        
        // Учитываем текущий scale родителя, чтобы localPosition работал корректно
        clone.transform.localPosition = new Vector3(offsetX / transform.localScale.x, 0, 0);
        clone.transform.localScale = Vector3.one;

        SpriteRenderer mySr = GetComponent<SpriteRenderer>();
        SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
        
        cloneSr.sprite = mySr.sprite;
        cloneSr.color = mySr.color;
        cloneSr.sortingLayerID = mySr.sortingLayerID;
        cloneSr.sortingLayerName = mySr.sortingLayerName;
        cloneSr.sortingOrder = mySr.sortingOrder;
        cloneSr.drawMode = mySr.drawMode;
        cloneSr.size = mySr.size;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Автоскролл сдвигает "стартовую" позицию
        startPos.x += autoScrollSpeedX * Time.deltaTime;

        // Вычисляем, насколько должен сдвинуться фон относительно стартовой позиции
        float distX = (cam.position.x * parallaxMultiplier.x);
        float distY = (cam.position.y * parallaxMultiplier.y);

        // Применяем позицию
        transform.position = new Vector3(startPos.x + distX, startPos.y + distY, transform.position.z);

        // Бесконечный цикл: проверяем, не пора ли перепрыгнуть
        if (infiniteLoopX)
        {
            // tempX показывает, насколько камера уехала относительно самого фона
            float tempX = (cam.position.x * (1 - parallaxMultiplier.x));
            float relativeDist = tempX - startPos.x;

            if (relativeDist > lengthX)
            {
                startPos.x += lengthX;
            }
            else if (relativeDist < -lengthX)
            {
                startPos.x -= lengthX;
            }
        }
    }
}
