using UnityEngine;

/// <summary>
/// Позволяет запускать сцену с нужного уровня (Grid'а) для отладки.
/// Повесьте на GameCamera или на отдельный пустой объект.
/// В инспекторе настройте список уровней (гридов) и выберите стартовый индекс.
/// </summary>
public class DebugLevelSelector : MonoBehaviour
{
    [System.Serializable]
    public class DebugLevel
    {
        public string name;              // Имя для удобства ("Level 1 - Variables", "Level 2 - Elevators")
        public Grid grid;                // Ссылка на Grid объект этого уровня
        public Vector3 cameraOffset;     // Смещение камеры относительно центра грида (обычно 0, 0, -10)
        public Vector3 playerStartPos;   // Мировая позиция старта игрока на этом уровне
    }

    [Header("Debug Settings")]
    [Tooltip("Включить выбор уровня? Отключите перед билдом!")]
    public bool enableDebugSelector = true;

    [Tooltip("Индекс уровня, с которого запустится сцена (0 = первый)")]
    public int startLevelIndex = 0;

    [Header("Levels")]
    public DebugLevel[] levels;

    [Header("References")]
    public Camera gameCamera;
    public PlayerController player;

    private int currentLevelIndex = -1;

    void Start()
    {
        if (!enableDebugSelector) return;

        // Автопоиск, если не назначено в инспекторе
        if (gameCamera == null)
            gameCamera = Camera.main;
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("[DebugLevelSelector] Не настроены уровни!");
            return;
        }

        // Ограничиваем индекс
        startLevelIndex = Mathf.Clamp(startLevelIndex, 0, levels.Length - 1);

        SwitchToLevel(startLevelIndex);
    }

    void Update()
    {
        if (!enableDebugSelector) return;

        // Горячие клавиши для быстрого переключения (только в Editor)
        #if UNITY_EDITOR
        // F1, F2, F3... для переключения между уровнями
        for (int i = 0; i < Mathf.Min(levels.Length, 12); i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                SwitchToLevel(i);
                break;
            }
        }
        #endif
    }

    public void SwitchToLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length) return;
        if (index == currentLevelIndex) return;

        currentLevelIndex = index;
        DebugLevel level = levels[index];

        Debug.Log($"[DebugLevelSelector] Переключаюсь на уровень {index}: {level.name}");

        // 1) Перемещаем камеру
        if (gameCamera != null && level.grid != null)
        {
            Vector3 gridCenter = level.grid.transform.position;
            Vector3 cameraPos = gridCenter + level.cameraOffset;
            
            // Сохраняем Z камеры, если offset не задан
            if (level.cameraOffset == Vector3.zero)
            {
                cameraPos = new Vector3(gridCenter.x, gridCenter.y, gameCamera.transform.position.z);
            }

            gameCamera.transform.position = cameraPos;
            Debug.Log($"[DebugLevelSelector] Камера -> {cameraPos}");
        }

        // 2) Перемещаем игрока
        if (player != null)
        {
            player.SetStartPosition(level.playerStartPos);
            Debug.Log($"[DebugLevelSelector] Игрок -> {level.playerStartPos}");
        }

        // 3) Переключаем Grid в GridMovementController,
        //    чтобы расчёт координат шёл по правильному гриду
        if (player != null && level.grid != null)
        {
            GridMovementController gridMovement = player.GetComponent<GridMovementController>();
            if (gridMovement != null)
            {
                gridMovement.grid = level.grid;
                Debug.Log($"[DebugLevelSelector] GridMovementController.grid -> {level.grid.name}");
            }
        }
    }

    // Красивый GUI в инспекторе для отображения текущего уровня
    void OnGUI()
    {
        if (!enableDebugSelector) return;

        #if UNITY_EDITOR
        // Маленький лейбл в углу экрана
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        string levelName = (levels != null && currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
            ? levels[currentLevelIndex].name
            : "N/A";

        GUI.Label(new Rect(10, 10, 400, 30), $"[DEBUG] Level: {levelName} (F1-F{levels?.Length ?? 0} to switch)", style);
        #endif
    }
}
