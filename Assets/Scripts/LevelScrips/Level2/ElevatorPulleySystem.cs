using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Система лебёдки с правильной схемой цепей:
///   - Горизонтальная цепь между двумя шкивами
///   - V-образная цепь от левого шкива к двум краям платформы
///   - Прямая цепь от правого шкива к грузу (1 якорь)
///
/// Иерархия:
///   Elevator_1  (этот скрипт)
///     ├── WheelLeft             (шкив слева)
///     ├── WheelRight            (шкив справа)
///     ├── PlatformSide          (двигается ВВЕРХ)
///     │   ├── Platform          (спрайт доски)
///     │   ├── AnchorLeft        (левый край платформы — пустой объект)
///     │   └── AnchorRight       (правый край платформы — пустой объект)
///     ├── WeightSide            (двигается ВНИЗ)
///     │   ├── WeightVisual      (спрайт ящика)
///     │   └── Anchor            (верх ящика — пустой объект)
///     └── ChainContainer        (пустой, сюда спавнятся звенья)
/// </summary>
public class ElevatorPulleySystem : MonoBehaviour
{
    [Header("=== Стороны лебёдки ===")]
    public Transform platformSide;
    public Transform weightSide;

    [Header("=== Шкивы (колёса наверху) ===")]
    public Transform wheelLeft;
    public Transform wheelRight;

    [Header("=== Якоря платформы ===")]
    [Tooltip("Точка разделения цепи (где прямой участок переходит в V-образный).")]
    public Transform platformSplitPoint;
    [Tooltip("Левый край платформы")]
    public Transform platformAnchorLeft;
    [Tooltip("Правый край платформы")]
    public Transform platformAnchorRight;

    [Header("=== Якорь груза (прямая цепь) ===")]
    [Tooltip("Верх ящика с грузом")]
    public Transform weightAnchor;

    [Header("=== Цепь ===")]
    public Sprite chainLinkSprite;
    [Tooltip("Высота одного звена в юнитах")]
    public float chainLinkHeight = 0.3f;
    public Transform chainContainer;
    public int chainSortingOrder = -1;
    [Tooltip("Sorting Layer для цепей")]
    public string chainSortingLayer = "Default";

    [Header("=== Анимация ===")]
    public float moveSpeed = 2f;
    public float travelDistance = 5f;

    [Header("=== Аудио ===")]
    [Tooltip("Звук движения лебедки (зацикленный)")]
    public AudioClip moveSound;
    private AudioSource audioSource;

    [Header("=== Состояние ===")]
    public float requiredWeight = 10f;
    private float currentWeight = 0f;
    public float CurrentWeight => currentWeight;
    private bool isAnimating = false;

    /// <summary>
    /// Доля пройденного пути (0..1). 0 = внизу, 1 = полностью наверху.
    /// </summary>
    public float Progress => Mathf.Clamp01(currentWeight / Mathf.Max(requiredWeight, 0.01f));

    /// <summary>
    /// true, когда лифт доехал до конца И не анимируется.
    /// </summary>
    public bool IsFinished => Progress >= 1f && !isAnimating;

    // Начальные позиции
    private Vector3 platformStartLocal;
    private Vector3 weightStartLocal;

    // Текущая «визуальная» доля пути (к ней анимируемся)
    private float currentVisualProgress = 0f;

    // Звенья для сегментов цепи
    private List<GameObject> chainHorizontal = new List<GameObject>();   // между шкивами
    private List<GameObject> chainVertical = new List<GameObject>();     // шкив → точка разделения (прямой участок "a")
    private List<GameObject> chainLeftArm = new List<GameObject>();      // точка разделения → левый якорь
    private List<GameObject> chainRightArm = new List<GameObject>();     // точка разделения → правый якорь
    private List<GameObject> chainWeight = new List<GameObject>();       // шкив → якорь груза

    // Тексты для визуализации веса
    private TextMeshPro platformWeightText;
    private TextMeshPro addedWeightText;

    void Start()
    {
        if (platformSide != null) platformStartLocal = platformSide.localPosition;
        if (weightSide != null) weightStartLocal = weightSide.localPosition;

        RebuildAllChains();

        // Ищем конкретные объекты камней, чтобы центрировать текст идеально
        if (platformSide != null)
        {
            Transform constWeightBox = platformSide.Find("Elevator Weight Const");
            if (constWeightBox != null)
            {
                platformWeightText = CreateWeightText(constWeightBox, requiredWeight.ToString());
                platformWeightText.transform.localPosition = Vector3.zero;
            }
        }

        if (weightSide != null)
        {
            Transform dynamicWeightBox = weightSide.Find("Elevator Weight");
            if (dynamicWeightBox != null)
            {
                addedWeightText = CreateWeightText(dynamicWeightBox, "0");
                addedWeightText.transform.localPosition = Vector3.zero;
            }
        }
        // Настраиваем AudioSource для звука лебедки
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        // Можно сделать spatialBlend = 1f, если нужна 3D-локализация (звук затухает вдалеке), 
        // но оставим 0f (2D) для слышимости
        audioSource.spatialBlend = 0f; 
        audioSource.volume = 0.7f;
    }

    private TextMeshPro CreateWeightText(Transform parent, string initialText)
    {
        GameObject textObj = new GameObject("WeightText");
        textObj.transform.SetParent(parent, false); // Идеальный центр
        
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = initialText;
        textMesh.fontSize = 5;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Черный или тёмно-серый цвет хорошо читается на камне
        textMesh.color = new Color(0.2f, 0.2f, 0.2f, 1f); 
        textMesh.sortingOrder = 50;

        // Берём шрифт 100% правильно из компонента логики уровней
        LevelManager lm = FindFirstObjectByType<LevelManager>();
        if (lm != null && lm.taskTitle != null && lm.taskTitle.font != null)
        {
            textMesh.font = lm.taskTitle.font;
        }

        return textMesh;
    }

    /// <summary>
    /// Добавить груз. Лифт сдвинется пропорционально:
    /// requiredWeight=10, добавили 5 → едет на 50% пути.
    /// Добавили ещё 3 → доезжает до 80%. И т.д.
    /// </summary>
    public void AddWeight(float weight)
    {
        currentWeight += weight;
        // Ограничиваем максимум
        currentWeight = Mathf.Min(currentWeight, requiredWeight);

        if (addedWeightText != null)
        {
            addedWeightText.text = currentWeight.ToString();
        }

        float targetProgress = Progress;
        Debug.Log($"[Pulley] Добавлен груз {weight}. Итого: {currentWeight}/{requiredWeight} ({targetProgress * 100:F0}%)");

        // Запускаем анимацию от текущей визуальной позиции к новой
        if (!isAnimating)
        {
            StartCoroutine(AnimateToProgress(targetProgress));
        }
    }

    /// <summary>
    /// Принудительно задать прогресс (0..1) и анимировать.
    /// </summary>
    public void SetProgress(float progress01)
    {
        currentWeight = Mathf.Clamp01(progress01) * requiredWeight;
        if (!isAnimating)
        {
            StartCoroutine(AnimateToProgress(progress01));
        }
    }

    private IEnumerator AnimateToProgress(float targetProgress)
    {
        isAnimating = true;

        if (moveSound != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.clip = moveSound;
            audioSource.Play();
        }

        float startProgress = currentVisualProgress;
        float progressDelta = Mathf.Abs(targetProgress - startProgress);

        // Время анимации пропорционально расстоянию
        float distance = progressDelta * travelDistance;
        float duration = distance / Mathf.Max(moveSpeed, 0.1f);
        // Минимум 0.3 секунды, чтобы было заметно
        duration = Mathf.Max(duration, 0.3f);

        float elapsed = 0f;
        float wheelRotSpeed = 180f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            currentVisualProgress = Mathf.Lerp(startProgress, targetProgress, t);
            ApplyPositions(currentVisualProgress);

            RebuildAllChains();
            yield return null;
        }

        currentVisualProgress = targetProgress;
        ApplyPositions(currentVisualProgress);
        RebuildAllChains();

        // Остановка звука
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isAnimating = false;

        string status = targetProgress >= 1f ? "Полностью наверху!" : $"{targetProgress * 100:F0}% пути";
        Debug.Log($"[Pulley] Анимация завершена. {status}");
    }

    /// <summary>
    /// Устанавливает позиции платформы и груза по доле пути (0..1).
    /// </summary>
    private void ApplyPositions(float progress)
    {
        if (platformSide != null)
            platformSide.localPosition = platformStartLocal + Vector3.up * (travelDistance * progress);
        if (weightSide != null)
            weightSide.localPosition = weightStartLocal + Vector3.down * (travelDistance * progress);
    }

    // ========== ЦЕПИ ==========

    private void RebuildAllChains()
    {
        if (chainLinkSprite == null) return;

        // 1) Горизонтальная цепь: WheelLeft → WheelRight
        if (wheelLeft != null && wheelRight != null)
        {
            BuildChainSegment(wheelLeft.position, wheelRight.position, ref chainHorizontal);
        }

        // Определяем точку разделения:
        // Если splitPoint назначен — берём его мировую позицию.
        // Если нет — вычисляем автоматически (посередине между якорями, чуть выше).
        Vector3 splitPos;
        if (platformSplitPoint != null)
        {
            splitPos = platformSplitPoint.position;
        }
        else if (platformAnchorLeft != null && platformAnchorRight != null)
        {
            Vector3 mid = (platformAnchorLeft.position + platformAnchorRight.position) / 2f;
            splitPos = mid + Vector3.up * 1f; // на 1 юнит выше середины платформы
        }
        else
        {
            splitPos = wheelLeft != null ? wheelLeft.position + Vector3.down * 2f : Vector3.zero;
        }

        // Отступ, чтобы звенья разных сегментов не выпирали в точке схождения
        float margin = chainLinkHeight * 0.1f;

        // 2) Вертикальный участок "a": от шкива вниз до точки разделения
        if (wheelLeft != null)
        {
            // Заканчиваем чуть ВЫШЕ splitPos
            Vector3 vertEnd = splitPos + (wheelLeft.position - splitPos).normalized * margin;
            BuildChainSegment(vertEnd, wheelLeft.position, ref chainVertical);
        }

        // 3) Левая рука: от точки разделения к левому краю платформы
        if (platformAnchorLeft != null)
        {
            // Начинаем чуть НИЖЕ splitPos (в сторону якоря)
            Vector3 armStart = splitPos + (platformAnchorLeft.position - splitPos).normalized * margin;
            BuildChainSegment(platformAnchorLeft.position, armStart, ref chainLeftArm);
        }

        // 4) Правая рука: от точки разделения к правому краю платформы
        if (platformAnchorRight != null)
        {
            Vector3 armStart = splitPos + (platformAnchorRight.position - splitPos).normalized * margin;
            BuildChainSegment(platformAnchorRight.position, armStart, ref chainRightArm);
        }

        // 5) Цепь груза: WheelRight → weightAnchor
        if (wheelRight != null && weightAnchor != null)
        {
            BuildChainSegment(weightAnchor.position, wheelRight.position, ref chainWeight);
        }
    }

    private void BuildChainSegment(Vector3 from, Vector3 to, ref List<GameObject> links)
    {
        float totalDist = Vector3.Distance(from, to);
        int count = Mathf.Max(1, Mathf.CeilToInt(totalDist / chainLinkHeight));

        // Создаём недостающие звенья
        while (links.Count < count)
        {
            GameObject link = new GameObject($"chain_{links.Count}");
            link.transform.SetParent(chainContainer != null ? chainContainer : transform);

            SpriteRenderer sr = link.AddComponent<SpriteRenderer>();
            sr.sprite = chainLinkSprite;
            sr.sortingOrder = chainSortingOrder;
            sr.sortingLayerName = chainSortingLayer;

            links.Add(link);
        }

        // Показываем/скрываем
        for (int i = 0; i < links.Count; i++)
        {
            if (links[i] != null)
                links[i].SetActive(i < count);
        }

        // Размещаем
        Vector3 dir = (to - from).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        for (int i = 0; i < count && i < links.Count; i++)
        {
            if (links[i] == null) continue;

            float pos = (i + 0.5f) * chainLinkHeight;
            links[i].transform.position = from + dir * pos;
            links[i].transform.rotation = Quaternion.Euler(0, 0, angle);

            // Масштабируем спрайт под размер звена
            SpriteRenderer sr = links[i].GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float sprH = sr.sprite.bounds.size.y;
                float scale = chainLinkHeight / sprH;
                links[i].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    // ========== СБРОС ==========

    public void ResetElevator()
    {
        StopAllCoroutines();
        isAnimating = false;
        currentWeight = 0f;
        currentVisualProgress = 0f;

        if (platformSide != null) platformSide.localPosition = platformStartLocal;
        if (weightSide != null) weightSide.localPosition = weightStartLocal;

        RebuildAllChains();
        Debug.Log("[Pulley] Сброшен.");
    }

    // ========== ГИЗМО ==========

    void OnDrawGizmosSelected()
    {
        // Горизонтальная цепь
        Gizmos.color = Color.red;
        if (wheelLeft != null && wheelRight != null)
            Gizmos.DrawLine(wheelLeft.position, wheelRight.position);

        // Точка разделения
        Vector3 splitGizmo = Vector3.zero;
        if (platformSplitPoint != null)
            splitGizmo = platformSplitPoint.position;
        else if (platformAnchorLeft != null && platformAnchorRight != null)
            splitGizmo = (platformAnchorLeft.position + platformAnchorRight.position) / 2f + Vector3.up;

        // Вертикальный участок (шкив → splitPoint)
        Gizmos.color = Color.white;
        if (wheelLeft != null)
            Gizmos.DrawLine(wheelLeft.position, splitGizmo);

        // V-цепь (splitPoint → края платформы)
        Gizmos.color = Color.green;
        if (platformAnchorLeft != null)
            Gizmos.DrawLine(splitGizmo, platformAnchorLeft.position);
        if (platformAnchorRight != null)
            Gizmos.DrawLine(splitGizmo, platformAnchorRight.position);

        // Точка разделения — кружок
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(splitGizmo, 0.15f);

        // Цепь груза
        Gizmos.color = Color.cyan;
        if (wheelRight != null && weightAnchor != null)
            Gizmos.DrawLine(wheelRight.position, weightAnchor.position);

        // Шкивы
        Gizmos.color = Color.yellow;
        if (wheelLeft != null) Gizmos.DrawWireSphere(wheelLeft.position, 0.25f);
        if (wheelRight != null) Gizmos.DrawWireSphere(wheelRight.position, 0.25f);

        // Направления движения
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        if (platformSide != null)
            Gizmos.DrawLine(platformSide.position, platformSide.position + Vector3.up * travelDistance);
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if (weightSide != null)
            Gizmos.DrawLine(weightSide.position, weightSide.position + Vector3.down * travelDistance);
    }
}
