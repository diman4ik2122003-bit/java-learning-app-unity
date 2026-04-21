using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер всех лифтов на уровне.
/// Управляет несколькими ElevatorPulleySystem и связывает их с логикой уровня.
/// </summary>
public class ElevatorLevelController : MonoBehaviour
{
    [System.Serializable]
    public class ElevatorEntry
    {
        public int id;
        public ElevatorPulleySystem pulley;
    }

    [Header("Лифты")]
    public List<ElevatorEntry> elevators = new List<ElevatorEntry>();

    [Header("Игрок")]
    public PlayerController player;

    [Header("Gamification")]
    [Tooltip("Список известных типов. Оставьте пустым, чтобы первый сундук в катсцене выдал и показал первый тип!")]
    public List<string> unlockedTypes = new List<string>();

    [Header("Cutscene")]
    [Tooltip("Автоматически проиграть заход игрока и открытие первого сундука при старте")]
    public bool playIntroCutscene = true;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        if (playIntroCutscene && player != null)
        {
            StartCoroutine(PlayIntroCoroutine());
        }
    }

    private IEnumerator PlayIntroCoroutine()
    {
        // Ждем 1 кадр, чтобы LevelGameManager телепортировал игрока на startPlayerPosition
        yield return null;

        // Ищем первый сундук правее изначальной позиции
        ChestController firstChest = FindNextChestToRight(player.transform.position.x - 2f);
        if (firstChest != null)
        {
            Vector3 target = firstChest.transform.position;
            target.x -= 0.15f; // Доходим почти до середины
            target.y = player.transform.position.y; // Сохраняем исходную высоту, чтобы не провалиться под землю

            yield return MovePlayerSmoothly(target, 3f);

            // Даем небольшую паузу
            yield return new WaitForSeconds(0.2f);
            
            // Открываем автоматически (начнётся анимация и вылет текста)
            firstChest.Open();
        }
    }

    public void UnlockType(string typeName)
    {
        if (!unlockedTypes.Contains(typeName))
        {
            unlockedTypes.Add(typeName);
            Debug.Log($"[Gamification] Разблокирован новый тип: {typeName}");
            
            CodeEditor editor = FindFirstObjectByType<CodeEditor>();
            if (editor != null)
            {
                editor.AddConsoleLog($"🏆 Найден новый тип данных из сундука: {typeName}!", false);
            }

            LevelGameManager lm = LevelGameManager.Instance;
            if (lm != null)
            {
                lm.ReportProgress();
            }
        }
    }

    /// <summary>
    /// Добавить вес на лифт и запустить анимацию, если хватает.
    /// Вызывается из JavaCodeExecutor.
    /// </summary>
    public IEnumerator AddWeightToElevator(int id, long weight)
    {
        ElevatorEntry entry = elevators.Find(e => e.id == id);
        if (entry == null || entry.pulley == null)
        {
            Debug.LogError($"[ElevatorController] Лифт с id {id} не найден!");
            yield break;
        }

        // Если лифт уже отработал в прошлый раз — просто пропускаем его команду!
        if (entry.pulley.IsFinished)
        {
            Debug.Log($"[ElevatorController] Лифт {id} уже опущен. Команда пропущена.");
            yield break;
        }

        // --- ПРОВЕРКА ВЕСА ---
        long totalWeight;
        try 
        { 
            totalWeight = checked(entry.pulley.CurrentWeight + weight); 
        }
        catch (System.OverflowException) 
        { 
            totalWeight = long.MaxValue; 
        }

        if (totalWeight <= entry.pulley.requiredWeight)
        {
            CodeEditor editor = FindFirstObjectByType<CodeEditor>();
            if (editor != null)
            {
                editor.AddConsoleLog($"[!] ОШИБКА: Лифт {id} не сдвинется! Передан вес {weight}, но этого недостаточно для перевеса.", true);
            }
            
            JavaCodeExecutor executor = FindFirstObjectByType<JavaCodeExecutor>();
            if (executor != null)
            {
                executor.executionAborted = true; // Прерываем цепочку команд
            }

            yield break;
        }

        // 1. Идём к платформе лифта
        if (player != null && entry.pulley.platformSide != null)
        {
            Vector3 platformCenter = GetPlatformCenter(entry.pulley.platformSide);
            float platformX = platformCenter.x;
            
            // Шаг А: идём горизонтально до середины платформы
            if (Mathf.Abs(player.transform.position.x - platformX) > 0.1f)
            {
                Vector3 targetPathX = new Vector3(platformX, player.transform.position.y, player.transform.position.z);
                yield return MovePlayerSmoothly(targetPathX);
            }
            
            // Шаг Б: падаем вниз (встаём на платформу)
            float platformY = platformCenter.y + 1f; // +0.5f чтобы ноги не проваливались под текстуру
            if (player.transform.position.y > platformY + 0.1f)
            {
                Vector3 targetPathY = new Vector3(platformX, platformY, player.transform.position.z);
                yield return MovePlayerSmoothly(targetPathY, 15f); // Быстро падаем
            }

            player.transform.SetParent(entry.pulley.platformSide);
            Debug.Log($"[ElevatorController] Игрок привязан к платформе лифта {id}");
        }

        // 2. Лифт едет наверх
        entry.pulley.AddWeight(weight);
        while (entry.pulley != null && !entry.pulley.IsFinished)
        {
            yield return null;
        }

        // 3. Отвязываем и идем к сундуку (или цели)
        if (player != null)
        {
            player.transform.SetParent(null);
            
            float targetX = float.MaxValue;
            ChestController targetChest = FindNextChestToRight(player.transform.position.x);
            if (targetChest != null)
            {
                targetX = targetChest.transform.position.x;
            }
            else
            {
                LevelGameManager lm = FindFirstObjectByType<LevelGameManager>();
                if (lm != null && lm.goalTransform != null && lm.goalTransform.position.x > player.transform.position.x)
                {
                    targetX = lm.goalTransform.position.x;
                }
                else
                {
                    // Прямая попытка найти объект с тегом "Finish" или именем "Goal" на крайний случай
                    GameObject goalObj = GameObject.Find("Goal");
                    if (goalObj != null && goalObj.transform.position.x > player.transform.position.x)
                    {
                        targetX = goalObj.transform.position.x;
                    }
                }
            }

            if (targetX != float.MaxValue)
            {
                Vector3 targetPath = new Vector3(targetX, player.transform.position.y, player.transform.position.z);
                yield return MovePlayerSmoothly(targetPath);
                
                if (targetChest != null)
                {
                    targetChest.Open();
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            GridMovementController gridMovement = player.GetComponent<GridMovementController>();
            if (gridMovement != null)
            {
                gridMovement.SetLogicalGridPosition(gridMovement.WorldToGrid(player.transform.position));
            }
        }
    }

    /// <summary>
    /// Старый API для обратной совместимости.
    /// </summary>
    public IEnumerator RaiseElevator(int id, long weight)
    {
        yield return AddWeightToElevator(id, weight);
    }

    public void ResetLevel()
    {
        foreach (var entry in elevators)
        {
            if (entry.pulley != null)
                entry.pulley.ResetElevator();
        }

        // Сброс всех сундуков
        ChestController[] chests = FindObjectsByType<ChestController>(FindObjectsSortMode.None);
        foreach (var c in chests) c.ResetChest();

        // Сброс игрока
        if (player != null)
        {
            player.transform.SetParent(null);
            player.ResetState();
        }

        unlockedTypes.Clear();
        
        if (playIntroCutscene && player != null)
        {
            StartCoroutine(PlayIntroCoroutine());
        }
    }

    private Vector3 GetPlatformCenter(Transform platformSide)
    {
        // Ищем визуальную платформу (в иерархии есть опечатка "Elevator Platfrom")
        Transform visual = platformSide.Find("Elevator Platfrom");
        if (visual == null) visual = platformSide.Find("Elevator Platform");
        if (visual == null && platformSide.childCount > 0) visual = platformSide.GetChild(0);
        
        return visual != null ? visual.position : platformSide.position;
    }

    private ChestController FindNextChestToRight(float startX)
    {
        ChestController[] chests = FindObjectsByType<ChestController>(FindObjectsSortMode.None);
        ChestController best = null;
        float minDist = float.MaxValue;
        
        foreach (var chest in chests)
        {
            if (chest.transform.position.x > startX + 0.1f)
            {
                float d = chest.transform.position.x - startX;
                if (d < minDist)
                {
                    minDist = d;
                    best = chest;
                }
            }
        }
        return best;
    }

    private IEnumerator MovePlayerSmoothly(Vector3 targetPos, float speed = 5f)
    {
        if (player == null) yield break;
        
        Animator anim = player.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalking", true);
        
        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (targetPos.x < player.transform.position.x) sr.flipX = true;
            else if (targetPos.x > player.transform.position.x) sr.flipX = false;
        }

        while (Vector3.Distance(player.transform.position, targetPos) > 0.05f)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        player.transform.position = targetPos;
        
        if (anim != null) anim.SetBool("isWalking", false);
    }
}
