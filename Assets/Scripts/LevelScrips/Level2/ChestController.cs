using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class ChestController : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Положите сюда 7 кадров анимации открытия (от закрытого до открытого)")]
    public Sprite[] openFrames;
    [Tooltip("Задержка между кадрами (в секундах)")]
    public float frameRate = 0.1f;

    [Header("Interaction")]
    [Tooltip("Дистанция, с которой игрок может открыть сундук")]
    public float interactDistance = 1.5f;
    [Tooltip("Открывать ли сундук по нажатию кнопки (например E)? Если нет, можно открывать только из кода.")]
    public bool openWithKey = true;
    public KeyCode interactKey = KeyCode.E;

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;
    private PlayerController player;
    
    [Header("Gamification")]
    [Tooltip("Тип данных, который выдает этот сундук (например, short, int, long). Оставьте пустым, если сундук ничего не выдает.")]
    public string grantedType = "";
    
    [Header("Audio")]
    [Tooltip("Звук открытия сундука")]
    public AudioClip openSound;
    
    [Tooltip("Шрифт для вылетающего текста. Положите сюда alagard-12px.")]
    public TMP_FontAsset floatingTextFont;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = FindFirstObjectByType<PlayerController>();

        // Устанавливаем закрытый кадр по умолчанию
        if (openFrames != null && openFrames.Length > 0)
        {
            spriteRenderer.sprite = openFrames[0];
        }
    }

    private void Update()
    {
        // Проверяем возможность открытия с клавиатуры
        if (openWithKey && !isOpen && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            
            // Если игрок стоит на соседней клетке (или на той же)
            if (distance <= interactDistance)
            {
                // Тут можно было бы показать подсказку UI вроде "Нажмите E"
                
                if (Input.GetKeyDown(interactKey))
                {
                    Open();
                }
            }
        }
    }

    // Публичный метод, который можно вызывать из PlayerController или из Java кода!
    public void Open()
    {
        if (isOpen) return;
        
        isOpen = true;
        
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        StartCoroutine(PlayOpenAnimation());

        // Выдаем тип, если он прописан
        if (!string.IsNullOrEmpty(grantedType))
        {
            ElevatorLevelController elc = FindFirstObjectByType<ElevatorLevelController>();
            if (elc != null)
            {
                elc.UnlockType(grantedType);
            }
        }

        Debug.Log("[OK] Сундук открыт!");
        
        if (ConsoleController.Instance != null)
        {
            ConsoleController.Log("[OK] Сундук открыт!");
        }
    }

    private IEnumerator PlayOpenAnimation()
    {
        if (openFrames == null || openFrames.Length == 0)
        {
            Debug.LogWarning("[ChestController] Не назначены кадры анимации (openFrames)!");
            yield break;
        }

        // Перебираем спрайты от 0 до конца
        for (int i = 0; i < openFrames.Length; i++)
        {
            spriteRenderer.sprite = openFrames[i];
            yield return new WaitForSeconds(frameRate);
        }

        if (!string.IsNullOrEmpty(grantedType))
        {
            StartCoroutine(ShowFloatingText());
        }
    }

    private IEnumerator ShowFloatingText()
    {
        // Создаем пустой объект для текста
        GameObject floatingObj = new GameObject("FloatingText_" + grantedType);
        floatingObj.transform.position = transform.position + Vector3.up * 0.5f;

        // Создаем лучи света (имитация свечения)
        GameObject lightObj = new GameObject("Glow");
        lightObj.transform.SetParent(floatingObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        SpriteRenderer glowSr = lightObj.AddComponent<SpriteRenderer>();
        // В качестве простой магии используем стандартный спрайт круга из Unity или просто окрашиваем вершину
        // Так как мы не знаем какие есть спрайты, используем текст как основу всей магии
        
        // Добавляем текст
        TextMeshPro textMesh = floatingObj.AddComponent<TextMeshPro>();
        
        // Нормальный способ: берем шрифт из инспектора
        if (floatingTextFont != null)
        {
            textMesh.font = floatingTextFont;
        }

        textMesh.text = $"<size=50%>получен тип</size>\n<size=120%><b><color=#FFD700>{grantedType}</color></b></size>";
        textMesh.fontSize = 5;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = new Color(1f, 1f, 1f, 0f); // Начинаем прозрачным
        textMesh.sortingOrder = 100; // Поверх всего

        float duration = 2.5f;
        float elapsed = 0f;
        Vector3 startPos = floatingObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Плавный полет вверх
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            floatingObj.transform.position = Vector3.Lerp(startPos, endPos, easeT);
            
            // Плавное появление и затухание
            Color c = textMesh.color;
            if (t < 0.2f)
            {
                c.a = t / 0.2f; // Fade in
            }
            else if (t > 0.7f)
            {
                c.a = 1f - ((t - 0.7f) / 0.3f); // Fade out
            }
            else
            {
                c.a = 1f;
            }
            textMesh.color = c;
            
            // Легкое покачивание (пульсация масштаба)
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f * (1f - t);
            floatingObj.transform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }

        Destroy(floatingObj);
    }

    // Удобный метод для кнопки "Reset"
    public void ResetChest()
    {
        isOpen = false;
        if (openFrames != null && openFrames.Length > 0)
        {
            spriteRenderer.sprite = openFrames[0];
        }
    }
}
