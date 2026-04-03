using UnityEngine;
using System.Collections;

public class StatisticsButtonAnimation : MonoBehaviour
{
    public Transform quillTransform; 

    [Header("Настройки области")]
    public float writingWidth = 200f;  // Ширина строки
    public float writingHeight = 100f; // Расстояние между верхней и нижней строкой
    public Vector2 centerOffset;       // Сдвиг всей анимации

    [Header("Тайминги")]
    public float stepDuration = 0.8f;  // Длительность каждого шага (всего 3 шага)
    public float rotationAngle = 15f;  // Наклон пера

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private bool isAnimating = false;

    void Awake()
    {
        if (quillTransform != null)
        {
            startLocalPos = quillTransform.localPosition;
            startLocalRot = quillTransform.localRotation;
        }
    }

    [ContextMenu("Play Simple Writing")]
    public void PlayWritingAnim()
    {
        if (!isAnimating && quillTransform != null)
            StartCoroutine(AnimateSimpleWriting());
    }

    IEnumerator AnimateSimpleWriting()
    {
        isAnimating = true;

        // Определяем ключевые точки относительно центра + оффсет
        Vector3 topLeft = new Vector3(-writingWidth / 2, writingHeight / 2, 0) + (Vector3)centerOffset;
        Vector3 topRight = new Vector3(writingWidth / 2, writingHeight / 2, 0) + (Vector3)centerOffset;
        Vector3 bottomLeft = new Vector3(-writingWidth / 2, -writingHeight / 2, 0) + (Vector3)centerOffset;
        Vector3 bottomRight = new Vector3(writingWidth / 2, -writingHeight / 2, 0) + (Vector3)centerOffset;

        // Устанавливаем перо в начало (верхний левый угол)
        quillTransform.localPosition = topLeft;

        // ЭТАП 1: Пишем слева направо (верхняя строка)
        yield return StartCoroutine(MoveQuill(topLeft, topRight, rotationAngle));

        // ЭТАП 2: Спускаемся вниз (переход на новую строку)
        yield return StartCoroutine(MoveQuill(topRight, bottomLeft, 0f));

        // ЭТАП 3: Пишем слева направо (нижняя строка)
        // --- ВОТ ТУТ МОЖНО ДОБАВИТЬ ЗВУК ---
        // Debug.Log("Звук второй строки");
        yield return StartCoroutine(MoveQuill(bottomLeft, bottomRight, rotationAngle));

        // Возврат в исходную позицию (над книгой)
        float elapsed = 0;
        Vector3 currentPos = quillTransform.localPosition;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            quillTransform.localPosition = Vector3.Lerp(currentPos, startLocalPos, elapsed / 0.4f);
            quillTransform.localRotation = Quaternion.Lerp(quillTransform.localRotation, startLocalRot, elapsed / 0.4f);
            yield return null;
        }

        isAnimating = false;
    }

    // Вспомогательный метод для плавного перемещения
    IEnumerator MoveQuill(Vector3 from, Vector3 to, float tilt)
    {
        float elapsed = 0;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / stepDuration;
            
            // Двигаем позицию
            quillTransform.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            
            // Наклоняем перо (только если есть наклон, иначе выравниваем)
            float currentTilt = (tilt != 0) ? Mathf.Sin(t * Mathf.PI) * tilt : 0;
            quillTransform.localRotation = startLocalRot * Quaternion.Euler(0, 0, currentTilt);
            
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 gizmoPos = transform.TransformPoint(new Vector3(centerOffset.x, centerOffset.y, 0));
        Gizmos.DrawWireCube(gizmoPos, new Vector3(writingWidth, writingHeight, 0.1f));
    }
}