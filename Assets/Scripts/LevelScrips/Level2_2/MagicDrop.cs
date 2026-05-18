using UnityEngine;
using System.Collections;

public class MagicDrop : MonoBehaviour
{
    [Tooltip("Если true, верхняя часть спрайта (носик) смотрит по ходу полета. Если false - назад (как настоящая летящая капля).")]
    public bool pointUpForward = false;

    [Header("Effects")]
    [Tooltip("Звук, который проиграется при попадании капли в котел")]
    public AudioClip dropSound;
    [Tooltip("Громкость звука (от 0 до 1)")]
    public float soundVolume = 0.8f;

    public void Fly(Vector3 startPoint, Transform targetTransform, float flightDuration, float arcHeight)
    {
        transform.position = startPoint;
        StartCoroutine(FlyRoutine(startPoint, targetTransform, flightDuration, arcHeight));
    }

    private IEnumerator FlyRoutine(Vector3 startPoint, Transform target, float duration, float arcHeight)
    {
        float elapsed = 0f;
        Vector3 previousPos = startPoint;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (target == null) break;

            // X и Z двигаются линейно к цели
            Vector3 currentPos = Vector3.Lerp(startPoint, target.position, t);
            
            // Y двигается по параболе (Mathf.Sin от 0 до PI дает плавную дугу вверх и вниз)
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            
            transform.position = currentPos;
            
            // ⭐ ПОВОРОТ КАПЛИ
            Vector3 direction = currentPos - previousPos;
            if (direction != Vector3.zero)
            {
                transform.up = pointUpForward ? direction.normalized : -direction.normalized;
            }
            previousPos = currentPos;

            yield return null; // ждем следующий кадр
        }

        // Тут капля долетела до котла
        if (dropSound != null)
        {
            // Проигрываем звук в точке котла, даже если капля уничтожится
            AudioSource.PlayClipAtPoint(dropSound, transform.position, soundVolume);
        }

        Destroy(gameObject);
    }
}
