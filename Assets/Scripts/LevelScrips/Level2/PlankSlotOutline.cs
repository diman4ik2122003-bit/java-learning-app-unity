using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlankSlotOutline : MonoBehaviour
{
    [Header("Настройки обводки")]
    public Color outlineColor = new Color(1f, 0.6f, 0f, 0.8f); // Оранжевый полупрозрачный
    public float lineWidth = 0.05f;

    [Header("Размер (если нет BoxCollider2D)")]
    public Vector2 size = new Vector2(1f, 0.2f); // Размер дощечки 1x0.2

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // Настроим LineRenderer для 2D-линий
        lineRenderer.positionCount = 5; // 4 угла + возврат в начальную точку, чтобы замкнуть контур
        lineRenderer.useWorldSpace = false; // Локальные координаты, чтобы двигать слот без изломов
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Обычный 2D материал
        lineRenderer.startColor = outlineColor;
        lineRenderer.endColor = outlineColor;

        // Пытаемся взять размер из коллайдера, если он висит на слоте
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            size = boxCol.size;
        }

        DrawOutline();
    }

    void DrawOutline()
    {
        // Вычисляем углы относительно центра (0,0) в локальных координатах
        float halfWidth = size.x / 2f;
        float halfHeight = size.y / 2f;

        Vector3 topLeft = new Vector3(-halfWidth, halfHeight, 0);
        Vector3 topRight = new Vector3(halfWidth, halfHeight, 0);
        Vector3 bottomRight = new Vector3(halfWidth, -halfHeight, 0);
        Vector3 bottomLeft = new Vector3(-halfWidth, -halfHeight, 0);

        // Устанавливаем точки (по часовой стрелке)
        lineRenderer.SetPosition(0, topLeft);
        lineRenderer.SetPosition(1, topRight);
        lineRenderer.SetPosition(2, bottomRight);
        lineRenderer.SetPosition(3, bottomLeft);
        lineRenderer.SetPosition(4, topLeft); // Замкнуть линию
    }

    // Если вы хотите видеть рамку даже в редакторе (до запуска игры)
    void OnDrawGizmos()
    {
        Gizmos.color = outlineColor;
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        Vector2 drawSize = boxCol != null ? boxCol.size : size;
        
        Gizmos.DrawWireCube(transform.position, drawSize);
    }
}
