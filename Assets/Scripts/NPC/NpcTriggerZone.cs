using UnityEngine;

/// <summary>
/// Невидимая зона-триггер: запускает NpcSequencer когда Player входит в коллайдер.
///
/// КАК НАСТРОИТЬ:
///  1. Создай пустой GameObject на сцене рядом с тем местом, где должна стартовать сцена.
///  2. Добавь BoxCollider2D (или CircleCollider2D), поставь галку Is Trigger.
///  3. Добавь этот скрипт, укажи sequencer.
///  4. Опционально: triggerOnce = true, чтобы сцена запускалась только один раз.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcTriggerZone : MonoBehaviour
{
    [Tooltip("Секвенсор, который будет запущен")]
    public NpcSequencer sequencer;

    [Tooltip("Если true — срабатывает только один раз за сессию")]
    public bool triggerOnce = true;

    [Tooltip("Шаг, с которого начать (0 = с самого начала)")]
    public int startFromStep = 0;

    private bool _triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && _triggered) return;
        if (other.GetComponent<PlayerController>() == null) return;

        _triggered = true;

        if (sequencer != null)
        {
            if (startFromStep > 0)
                sequencer.PlayFromStep(startFromStep);
            else
                sequencer.Play();
        }
    }

    /// <summary>Сброс флага (например, при рестарте уровня).</summary>
    public void ResetTrigger()
    {
        _triggered = false;
    }
}
