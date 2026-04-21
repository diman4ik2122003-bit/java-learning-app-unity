using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер уровня "Мост"
/// </summary>
public class BridgeLevelController : MonoBehaviour
{
    [Header("Настройки моста")]
    public int requiredPlanks = 8;
    [Tooltip("Объект-родитель, куда будут спавниться доски")]
    public Transform planksContainer;
    [Tooltip("Спрайт, который будет использоваться для доски (обязательно добавьте)")]
    public Sprite plankSprite;
    
    [Header("Геометрия пустых слотов")]
    [Tooltip("Пустые невидимые платформы, на которые игрок будет класть доски. Порядок слева направо.")]
    public List<Transform> emptySlots = new List<Transform>();

    [Header("Игрок и Катсцены")]
    public PlayerController player;
    public bool playIntroCutscene = true;

    [Header("Звук")]
    public AudioClip plankDropSound;

    // Ссылки на созданные объекты
    private List<GameObject> placedPlanksObjects = new List<GameObject>();
    private bool[] isSlotFilled;

    void Awake()
    {
        isSlotFilled = new bool[requiredPlanks];
        // Если забыли добавить слоты руками - попытаемся найти их как детей контейнера
        if (emptySlots.Count == 0 && planksContainer != null)
        {
            foreach (Transform t in planksContainer)
            {
                emptySlots.Add(t);
            }
        }
        
        if (emptySlots.Count != requiredPlanks)
        {
            Debug.LogWarning($"[BridgeController] Текущее число слотов {emptySlots.Count} не равно нужному числу {requiredPlanks}. Рекомендую использовать {requiredPlanks} шт.");
        }
        
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
        yield return null; // Ждём спавна плеера LevelManager-ом

        // Идем немного вправо к краю обрыва
        if (emptySlots.Count > 0 && emptySlots[0] != null)
        {
            Vector3 target = player.transform.position;
            // Идем до первого слота минус небольшое расстояние
            target.x = emptySlots[0].position.x - 1.5f; 
            
            yield return MovePlayerSmoothly(target, 4f);
        }
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

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        bool wasKinematic = false;
        if (rb != null) 
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true; // Отключаем гравитацию на время скриптовой ходьбы
            rb.linearVelocity = Vector2.zero;
        }

        while (Vector3.Distance(player.transform.position, targetPos) > 0.05f)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        player.transform.position = targetPos;
        
        if (rb != null) rb.isKinematic = wasKinematic;
        
        if (anim != null) anim.SetBool("isWalking", false);
    }

    /// <summary>
    /// Проверка, заполнен ли мост
    /// </summary>
    public bool IsBridgeComplete()
    {
        for (int i = 0; i < requiredPlanks; i++)
        {
            if (!isSlotFilled[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Добавление одной планки. Вызывается из JavaCodeExecutor
    /// </summary>
    public IEnumerator AddPlank(int slotIndex)
    {
        CodeEditor editor = FindFirstObjectByType<CodeEditor>();
        
        if (slotIndex < 0 || slotIndex >= requiredPlanks)
        {
            if (editor != null)
                editor.AddConsoleLog($"[!] ОШИБКА: Попытка положить планку в слот {slotIndex}. Доступно только слотов: 0 - {requiredPlanks - 1}", true);
                
            JavaCodeExecutor executor = FindFirstObjectByType<JavaCodeExecutor>();
            if (executor != null) executor.executionAborted = true; // Прерываем
            yield break;
        }

        if (isSlotFilled[slotIndex])
        {
            // Не ломаем код, но предупреждаем, что он делает лишнее действие
            if (editor != null)
                editor.AddConsoleLog($"⚠️ Внимание: Слот {slotIndex} уже заполнен. Планка падает вниз и теряется.");
            yield return new WaitForSeconds(0.2f);
            yield break; // Пропускаем
        }

        // --- СОЗДАНИЕ ПЛАНКИ ---
        isSlotFilled[slotIndex] = true;

        Vector3 targetPosition = Vector3.zero;
        if (slotIndex < emptySlots.Count && emptySlots[slotIndex] != null)
        {
            targetPosition = emptySlots[slotIndex].position;
        }
        else
        {
            // Fallback: просто смещаем вправо, если слоты забыли указать
            targetPosition = transform.position + new Vector3(slotIndex * 1f, 0, 0); 
        }

        GameObject plankObj = new GameObject($"Plank_{slotIndex}");
        plankObj.transform.SetParent(planksContainer, true);
        
        // Появляется сверху
        plankObj.transform.position = targetPosition + new Vector3(0, 3f, 0); 
        plankObj.transform.localScale = Vector3.zero;

        SpriteRenderer sr = plankObj.AddComponent<SpriteRenderer>();
        sr.sprite = plankSprite;
        sr.sortingOrder = 5; // Поверх окружения
        
        if (emptySlots.Count > slotIndex && emptySlots[slotIndex] != null)
        {
            // Устанавливаем масштаб (по X теперь 4, по Y 3)
            plankObj.transform.localScale = new Vector3(4f, 3f, 1f);
        }

        Vector3 finalScale = plankObj.transform.localScale == Vector3.zero ? new Vector3(4f, 3f, 1f) : plankObj.transform.localScale;
        plankObj.transform.localScale = Vector3.zero;

        placedPlanksObjects.Add(plankObj);

        // --- АНИМАЦИЯ ПАДЕНИЯ ---
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startPos = plankObj.transform.position;
        bool soundPlayed = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Easing: быстрый старт, плавная остановка (Ease Out Quad)
            float easeT = 1f - (1f - t) * (1f - t); 
            
            plankObj.transform.position = Vector3.Lerp(startPos, targetPosition, easeT);
            plankObj.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, easeT);
            
            // Если звук имеет пустую задержку в начале (тишину), играем его чуть заранее
            if (!soundPlayed && t >= 0.75f)
            {
                if (plankDropSound != null)
                {
                    AudioSource.PlayClipAtPoint(plankDropSound, targetPosition);
                }
                soundPlayed = true;
            }
            
            yield return null;
        }

        plankObj.transform.position = targetPosition;
        plankObj.transform.localScale = finalScale;
        
        if (!soundPlayed && plankDropSound != null)
        {
            AudioSource.PlayClipAtPoint(plankDropSound, targetPosition);
        }

        // Чтобы игрок мог по ней ходить:
        BoxCollider2D collider = plankObj.AddComponent<BoxCollider2D>();
        
        if (IsBridgeComplete())
        {
            if (editor != null)
                editor.AddConsoleLog("🟩 Все доски готовы! Пробуем перейти...", false);
        }

        yield return new WaitForSeconds(0.1f);
    }
    
    public IEnumerator WalkBridgeAndCheck()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (player == null || emptySlots.Count == 0) yield break;

        bool fell = false;

        // Идем по слотам проверить мост
        for (int i = 0; i < emptySlots.Count; i++)
        {
            Vector3 target = emptySlots[i].position;
            target.y = player.transform.position.y; // Идем строго ровно

            yield return MovePlayerSmoothly(target, 4f);

            if (!isSlotFilled[i])
            {
                // Игрок дошел до пустого слота - падение!
                fell = true;
                CodeEditor editor = FindFirstObjectByType<CodeEditor>();
                if (editor != null)
                    editor.AddConsoleLog($"❌ Ошибка: В слоте {i} нет дощечки! Игрок упал.", true);

                // Анимация падения
                Animator anim = player.GetComponentInChildren<Animator>();
                if (anim != null) anim.SetBool("isFalling", true);

                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = true; 
                    rb.linearVelocity = Vector2.zero;
                    
                    // Двигаем вниз
                    Vector3 fallTarget = player.transform.position;
                    fallTarget.y -= 15f; 
                    yield return MovePlayerSmoothly(fallTarget, 10f); // быстрое падение
                }
                else
                {
                    Vector3 fallTarget = player.transform.position;
                    fallTarget.y -= 15f; 
                    yield return MovePlayerSmoothly(fallTarget, 10f);
                }

                yield return new WaitForSeconds(0.5f);
                break;
            }
        }

        if (!fell)
        {
            Vector3 finalTarget;
            
            if (LevelGameManager.Instance != null && LevelGameManager.Instance.goalTransform != null)
            {
                finalTarget = LevelGameManager.Instance.goalTransform.position;
            }
            else
            {
                finalTarget = emptySlots[emptySlots.Count - 1].position;
                finalTarget.x += 2.5f; 
            }
            
            finalTarget.y = player.transform.position.y;
            yield return MovePlayerSmoothly(finalTarget, 4f);
            
            CodeEditor editor = FindFirstObjectByType<CodeEditor>();
            if (editor != null)
                editor.AddConsoleLog("🎉 Мост пройден! Уровень завершен.", false);
                
            if (LevelGameManager.Instance != null)
                LevelGameManager.Instance.progressMadeThisRun = true;
        }
    }
        


    /// <summary>
    /// Сброс уровня для новой попытки
    /// </summary>
    public void ResetLevel()
    {
        StopAllCoroutines();

        for (int i = 0; i < isSlotFilled.Length; i++)
        {
            isSlotFilled[i] = false;
        }

        for (int i = 0; i < placedPlanksObjects.Count; i++)
        {
            if (placedPlanksObjects[i] != null)
            {
                Destroy(placedPlanksObjects[i]);
            }
        }
        placedPlanksObjects.Clear();
        
        if (player != null)
        {
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isFalling", false);
            
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector2.zero;
            }
        }
        
        if (playIntroCutscene && player != null)
        {
            StartCoroutine(PlayIntroCoroutine());
        }
    }
}
