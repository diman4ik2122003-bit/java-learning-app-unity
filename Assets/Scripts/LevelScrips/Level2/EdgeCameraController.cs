using UnityEngine;

public class EdgeCameraController : MonoBehaviour
{
    [Header("Edge Panning")]
    [Tooltip("Скорость движения камеры при нахождении курсора у края")]
    public float panSpeed = 15f;
    [Tooltip("Толщина зоны у края экрана (в пикселях), реагирующая на курсор")]
    public float panBorderThickness = 15f;

    [Header("Drag Panning (Опционально)")]
    [Tooltip("Разрешить таскать камеру зажатой правой кнопкой мыши")]
    public bool enableDragPanning = true;
    
    [Header("Camera Limits")]
    [Tooltip("Включить ограничение зоны перемещения камеры")]
    public bool useLimits = true;
    [Tooltip("Минимальные координаты (Нижний левый угол)")]
    public Vector2 limitMin = new Vector2(-20f, -40f);
    [Tooltip("Максимальные координаты (Верхний правый угол)")]
    public Vector2 limitMax = new Vector2(30f, 10f);

    private Vector3 dragOrigin;

    void Update()
    {
        Vector3 pos = transform.position;

        // --- 1. Перемещение у краев экрана ---
        if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
        {
            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                pos.y += panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.y <= panBorderThickness)
            {
                pos.y -= panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                pos.x += panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x <= panBorderThickness)
            {
                pos.x -= panSpeed * Time.deltaTime;
            }
        }

        // --- 2. Перемещение перетаскиванием (Альтернатива) ---
        if (enableDragPanning)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) 
            {
                dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                Vector3 currentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 difference = dragOrigin - currentPos;
                pos += difference;
            }
        }

        // --- 3. Ограничиваем позицию (Clamp) ---
        if (useLimits)
        {
            pos.x = Mathf.Clamp(pos.x, limitMin.x, limitMax.x);
            pos.y = Mathf.Clamp(pos.y, limitMin.y, limitMax.y);
        }

        // Применяем новые координаты (Z не меняем)
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }
}
