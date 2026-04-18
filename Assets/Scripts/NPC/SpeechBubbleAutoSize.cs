using UnityEngine;
using TMPro;

/// <summary>
/// Вешается на SpeechBubble.
/// Автоматически подгоняет размер SpriteRenderer (9-sliced) под размер текста.
/// ПРИНУДИТЕЛЬНО удерживает выбранный угол (якорь/хвостик) в одной и той же точке мира.
/// </summary>
[ExecuteAlways]
public class SpeechBubbleAutoSize : MonoBehaviour
{
    public enum BubbleAnchor { 
        Free, 
        TopLeft, TopCenter, TopRight, 
        CenterLeft, Center, CenterRight, 
        BottomLeft, BottomCenter, BottomRight 
    }

    [Header("Ссылки")]
    public SpriteRenderer bubbleSprite;   // SpriteRenderer на этом же объекте
    public TextMeshPro bubbleText;        // TextMeshPro на дочернем BubbleText

    [Header("Настройки Якоря (Хвостика)")]
    [Tooltip("Выберите угол, который должен оставаться неподвижным (где находится хвостик)")]
    public BubbleAnchor anchor = BubbleAnchor.BottomRight;
    
    [Tooltip("Максимальная ширина (0 - без ограничений)")]
    public float maxWidth = 0f;

    [Header("Отступы вокруг текста")]
    public float paddingX = 0.4f;
    public float paddingY = 0.3f;

    [Header("Минимальный размер")]
    public float minWidth  = 0.5f;
    public float minHeight = 0.25f;

    [Header("Состояние (Не трогать)")]
    [SerializeField] private Vector3 _targetWorldPos; // Мировая точка, к которой привязан угол
    [SerializeField] private bool _anchorCaptured = false;

    void OnEnable()
    {
        // В редакторе сбрасываем захват, чтобы можно было подвигать бабл руками
        if (!Application.isPlaying) _anchorCaptured = false;
    }

    /// <summary>
    /// Зафиксировать ТЕКУЩЕЕ МИРОВОЕ положение выбранного угла.
    /// </summary>
    [ContextMenu("Capture Anchor Point")]
    public void CaptureAnchor()
    {
        if (bubbleSprite == null || bubbleSprite.sprite == null) return;
        
        // Считаем, где в мире сейчас находится выбранный угол
        Vector3 localOffset = GetLocalAnchorOffset(bubbleSprite.size);
        _targetWorldPos = transform.TransformPoint(localOffset);
        _anchorCaptured = true;
    }

    void LateUpdate()
    {
        if (bubbleSprite == null || bubbleText == null || bubbleSprite.sprite == null) return;

        // В начале игры/работы захватываем точку, которую настроил юзер
        if (Application.isPlaying && !_anchorCaptured)
        {
            CaptureAnchor();
        }

        // 1. Расчет размеров текста
        if (maxWidth > 0)
        {
            bubbleText.enableWordWrapping = true;
            bubbleText.rectTransform.sizeDelta = new Vector2(maxWidth - (paddingX * 2f), 10f);
        }
        else
        {
            bubbleText.enableWordWrapping = false;
        }

        bubbleText.ForceMeshUpdate();
        Vector2 textSize = bubbleText.GetRenderedValues(false);

        float w = Mathf.Max(textSize.x + paddingX * 2f, minWidth);
        float h = Mathf.Max(textSize.y + paddingY * 2f, minHeight);
        Vector2 newSize = new Vector2(w, h);

        // 2. Применяем размер спрайта
        bubbleSprite.size = newSize;

        // 3. Подгоняем текст под пузырь
        bubbleText.rectTransform.sizeDelta = new Vector2(w - paddingX * 2f, h - paddingY * 2f);

        // 4. ФИКСАЦИЯ: Двигаем трансформ, чтобы мировая позиция угла совпала с целью
        if (_anchorCaptured && anchor != BubbleAnchor.Free)
        {
            // Считаем, где угол находится СЕЙЧАС (после изменения size)
            Vector3 currentLocalOffset = GetLocalAnchorOffset(newSize);
            Vector3 currentWorldPos = transform.TransformPoint(currentLocalOffset);

            // На сколько нужно сдвинуть ВЕСЬ объект в мире, чтобы вернуть угол на место
            Vector3 offsetToMatch = _targetWorldPos - currentWorldPos;
            
            transform.position += offsetToMatch;
        }
    }

    // Возвращает смещение выбранного якоря относительно Transform.position в локальных координатах
    private Vector3 GetLocalAnchorOffset(Vector2 size)
    {
        // Берем нормализованный Pivot из спрайта (обычно 0.5, 0.5)
        Vector2 pivot = bubbleSprite.sprite.pivot / bubbleSprite.sprite.rect.size;
        
        string anchorStr = anchor.ToString();
        
        // Определяем множители для X и Y (-1..1 относительно пивота)
        float xMult = anchorStr.Contains("Right") ? 1f : (anchorStr.Contains("Left") ? 0f : 0.5f);
        float yMult = anchorStr.Contains("Top") ? 1f : (anchorStr.Contains("Bottom") ? 0f : 0.5f);

        float lx = (xMult - pivot.x) * size.x;
        float ly = (yMult - pivot.y) * size.y;

        return new Vector3(lx, ly, 0);
    }
}
