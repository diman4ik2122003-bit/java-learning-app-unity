using UnityEngine;

public class UniversalLevelManager : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData currentLevel;
    
    [Header("References")]
    public PlayerController player;
    public Transform goalTransform;
    public CodeEditor codeEditor;
    public JavaCodeExecutor executor;
    
    [Header("Scene Container")]
    public Transform levelSceneContainer; // родитель для спавна префабов уровня
    
    private GameObject currentLevelInstance;
    private bool levelCompleted = false;
    
    void Start()
    {
        if (currentLevel != null)
        {
            LoadLevel(currentLevel);
        }
    }
    
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("Level data is null!");
            return;
        }
        
        currentLevel = levelData;
        levelCompleted = false;
        
        // Очищаем консоль
        codeEditor?.ClearConsole();
        
        // Удаляем предыдущий уровень
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }
        
        // Спавним новый prefab уровня
        if (levelData.levelPrefab != null)
        {
            currentLevelInstance = Instantiate(
                levelData.levelPrefab,
                levelSceneContainer
            );
            
            Debug.Log($"Level prefab spawned: {levelData.levelPrefab.name}");
        }
        else
        {
            Debug.LogWarning("Level prefab is not assigned!");
        }
        
        // Позиционирование игрока и цели
        if (player != null)
        {
            player.transform.position = levelData.playerStartPosition;
            player.ResetState();
        }
        
        if (goalTransform != null)
        {
            goalTransform.position = levelData.goalPosition;
        }
        
        // UI
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog($"📖 {levelData.groupName}: {levelData.levelName}");
            codeEditor.AddConsoleLog($"🎯 {levelData.description}");
            codeEditor.AddConsoleLog(levelData.hint);
            codeEditor.SetCode(levelData.starterCode);
        }
    }
    
    void Update()
    {
        if (levelCompleted || currentLevel == null) return;
        
        // Проверка достижения цели
        if (player != null && goalTransform != null)
        {
            float distance = Vector3.Distance(player.transform.position, goalTransform.position);
            if (distance < 0.5f)
            {
                LevelComplete();
            }
        }
    }
    
    void LevelComplete()
    {
        levelCompleted = true;
        
        if (codeEditor != null)
        {
            codeEditor.AddConsoleLog("🎉 Поздравляю! Уровень пройден!");
            codeEditor.AddConsoleLog($"✨ Ты освоил: {currentLevel.levelName}");
        }
        
        Debug.Log($"Level {currentLevel.levelId} completed!");
        
        // TODO: Сохранить прогресс, показать экран победы
    }
    
    public LevelValidation[] GetValidations()
    {
        return currentLevel?.validations;
    }
    
    public void RestartLevel()
    {
        LoadLevel(currentLevel);
    }
}
