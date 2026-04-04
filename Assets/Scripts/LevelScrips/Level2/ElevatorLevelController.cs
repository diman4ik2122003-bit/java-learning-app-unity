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

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    /// <summary>
    /// Добавить вес на лифт и запустить анимацию, если хватает.
    /// Вызывается из JavaCodeExecutor.
    /// </summary>
    public IEnumerator AddWeightToElevator(int id, float weight)
    {
        ElevatorEntry entry = elevators.Find(e => e.id == id);
        if (entry == null || entry.pulley == null)
        {
            Debug.LogError($"[ElevatorController] Лифт с id {id} не найден!");
            yield break;
        }

        // --- ПРОВЕРКА ВЕСА ---
        float totalWeight = entry.pulley.CurrentWeight + weight;
        if (totalWeight < entry.pulley.requiredWeight)
        {
            CodeEditor editor = FindFirstObjectByType<CodeEditor>();
            if (editor != null)
            {
                editor.AddConsoleLog($"🔴 IllegalArgumentException: Лифт {id} не сдвинется! Передан вес {weight}, но этого недостаточно для перевеса.", true);
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
            float platformX = entry.pulley.platformSide.position.x;
            if (Mathf.Abs(player.transform.position.x - platformX) > 0.1f)
            {
                Vector3 targetPath = new Vector3(platformX, player.transform.position.y, player.transform.position.z);
                yield return MovePlayerSmoothly(targetPath);
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
                UniversalLevelManager mgr = FindFirstObjectByType<UniversalLevelManager>();
                if (mgr != null && mgr.goalTransform != null && mgr.goalTransform.position.x > player.transform.position.x)
                {
                    targetX = mgr.goalTransform.position.x;
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
                gridMovement.SetGridPosition(gridMovement.WorldToGrid(player.transform.position));
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
