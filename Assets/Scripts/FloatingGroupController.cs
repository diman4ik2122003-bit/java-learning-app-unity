using UnityEngine;
using UnityEngine.UI;

public class FloatingGroupController : MonoBehaviour
{
    // Настройки диапазона движения и скорости для всех элементов
    public Vector3 minOffset = new Vector3(0, 5f, 0);
    public Vector3 maxOffset = new Vector3(0, 20f, 0);
    public float minSpeed = 0.5f;
    public float maxSpeed = 2f;

    void Start()
    {
        // Ищем все дочерние Image
        var images = GetComponentsInChildren<Image>(includeInactive: true);

        foreach (var img in images)
        {
            // Если у Image уже есть скрипт, пропускаем
            if (img.GetComponent<FloatElement>() != null)
                continue;

            // Добавляем скрипт
            var flt = img.gameObject.AddComponent<FloatElement>();

            // Рандомизируем параметры
            flt.floatOffset = Vector3.Lerp(minOffset, maxOffset, Random.value);
            flt.floatSpeed = Random.Range(minSpeed, maxSpeed);
            flt.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }
}
