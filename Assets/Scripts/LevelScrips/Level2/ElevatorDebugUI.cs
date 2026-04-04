using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Дебаг-панель для тестирования лифтов прямо в Play Mode.
/// Повесьте на любой объект в сцене. Создаст UI автоматически.
/// </summary>
public class ElevatorDebugUI : MonoBehaviour
{
    [Header("Ссылка на контроллер лифтов")]
    public ElevatorLevelController elevatorController;

    [Header("Или напрямую на один лифт")]
    public ElevatorPulleySystem singlePulley;

    [Header("Настройки")]
    [Tooltip("ID лифта (для ElevatorLevelController)")]
    public int elevatorId = 1;

    [Tooltip("Вес, который добавляется по кнопке")]
    public float debugWeight = 10f;

    void OnGUI()
    {
        // Простой GUI — не требует Canvas и настройки в инспекторе
        GUILayout.BeginArea(new Rect(10, Screen.height - 160, 250, 150));

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        GUILayout.BeginVertical(boxStyle);

        GUILayout.Label($"=== Elevator Debug ===");
        GUILayout.Label($"Elevator ID: {elevatorId}");

        // Поле ввода веса
        GUILayout.BeginHorizontal();
        GUILayout.Label("Weight:", GUILayout.Width(55));
        string weightStr = GUILayout.TextField(debugWeight.ToString(), GUILayout.Width(60));
        if (float.TryParse(weightStr, out float parsed))
            debugWeight = parsed;
        GUILayout.EndHorizontal();

        // Кнопка "Добавить вес"
        if (GUILayout.Button($"Add {debugWeight} weight"))
        {
            AddWeight();
        }

        // Кнопка "Сбросить"
        if (GUILayout.Button("Reset Elevator"))
        {
            ResetElevator();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void AddWeight()
    {
        // Вариант 1: Через контроллер (по ID)
        if (elevatorController != null)
        {
            StartCoroutine(elevatorController.AddWeightToElevator(elevatorId, debugWeight));
            Debug.Log($"[DebugUI] Добавляю {debugWeight} на лифт {elevatorId} через контроллер");
            return;
        }

        // Вариант 2: Напрямую в один лифт
        if (singlePulley != null)
        {
            singlePulley.AddWeight(debugWeight);
            Debug.Log($"[DebugUI] Добавляю {debugWeight} напрямую в PulleySystem");
            return;
        }

        Debug.LogError("[DebugUI] Не назначен ни ElevatorLevelController, ни ElevatorPulleySystem!");
    }

    void ResetElevator()
    {
        if (elevatorController != null)
        {
            elevatorController.ResetLevel();
            Debug.Log("[DebugUI] Сброс через контроллер");
            return;
        }

        if (singlePulley != null)
        {
            singlePulley.ResetElevator();
            Debug.Log("[DebugUI] Сброс PulleySystem");
            return;
        }
    }
}
