using UnityEngine;
using System.Collections;

public class AchievmentsButtonAnimation : MonoBehaviour
{
    [Header("Настройки движения")]
    public Transform swordTransform; // Ссылка на объект меча
    public float liftHeight = 0.5f;   // На какую высоту поднимается меч
    public float liftDuration = 0.8f; // Длительность подъема (плавно)
    public float snapDuration = 0.1f; // Длительность возврата (резко)

    private Vector3 originalPosition;
    private bool isAnimating = false;

    void Start()
    {
        if (swordTransform != null)
        {
            originalPosition = swordTransform.localPosition;
        }
    }

    // Запустить анимацию можно через кнопку или другой скрипт
    [ContextMenu("Play Animation")]
    public void PlaySwordAnim()
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateSword());
        }
    }

    IEnumerator AnimateSword()
    {
        isAnimating = true;
        Vector3 targetPosition = originalPosition + new Vector3(0, liftHeight, 0);

        // 1. Плавный подъем (из ножен)
        float elapsed = 0;
        while (elapsed < liftDuration)
        {
            swordTransform.localPosition = Vector3.Lerp(originalPosition, targetPosition, Mathf.SmoothStep(0, 1, elapsed / liftDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        swordTransform.localPosition = targetPosition;

        // Небольшая пауза в верхней точке для акцента
        yield return new WaitForSeconds(0.05f);

        // 2. Резкий возврат (вкладывание)
        elapsed = 0;
        
        // --- МЕСТО ДЛЯ ВОСПРОИЗВЕДЕНИЯ ЗВУКА (Начало вкладывания) ---
        // AudioSource.PlayClipAtPoint(sheathSound, transform.position);
        Debug.Log("Звук вкладывания меча!"); 

        while (elapsed < snapDuration)
        {
            // Используем Lerp без сглаживания для эффекта "удара"
            swordTransform.localPosition = Vector3.Lerp(targetPosition, originalPosition, elapsed / snapDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        swordTransform.localPosition = originalPosition;
        isAnimating = false;
    }
}
