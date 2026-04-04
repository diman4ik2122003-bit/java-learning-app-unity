using UnityEngine;
using System.Collections;

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
        StartCoroutine(PlayOpenAnimation());

        // Место для вашей логики:
        // Выдать награду, сообщить LevelManager о победе, включить звук
        Debug.Log("🎉 Сундук открыт!");
        
        if (ConsoleController.Instance != null)
        {
            ConsoleController.Log("🎉 Сундук открыт!");
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
