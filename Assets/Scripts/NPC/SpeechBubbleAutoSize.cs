using UnityEngine;
using TMPro;

/// <summary>
/// Вешается на SpeechBubble.
/// Автоматически подгоняет размер SpriteRenderer (9-sliced) под размер текста.
/// </summary>
[ExecuteAlways]
public class SpeechBubbleAutoSize : MonoBehaviour
{
    [Header("Ссылки")]
    public SpriteRenderer bubbleSprite;   // SpriteRenderer на этом же объекте
    public TextMeshPro bubbleText;        // TextMeshPro на дочернем BubbleText

    [Header("Отступы вокруг текста")]
    public float paddingX = 0.4f;
    public float paddingY = 0.3f;

    [Header("Минимальный размер пузыря")]
    public float minWidth  = 1.5f;
    public float minHeight = 0.6f;

    void LateUpdate()
    {
        if (bubbleSprite == null || bubbleText == null) return;

        // Берём реальный размер текста
        Vector2 textSize = bubbleText.GetRenderedValues(onlyVisibleCharacters: false);

        float w = Mathf.Max(textSize.x + paddingX * 2f, minWidth);
        float h = Mathf.Max(textSize.y + paddingY * 2f, minHeight);

        // Растягиваем пузырь
        bubbleSprite.size = new Vector2(w, h);

        // Подгоняем RectTransform текста под новый размер пузыря
        bubbleText.rectTransform.sizeDelta = new Vector2(w - paddingX * 2f, h - paddingY * 2f);
    }
}
