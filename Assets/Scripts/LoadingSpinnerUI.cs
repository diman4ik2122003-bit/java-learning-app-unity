using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Спиннер загрузки для uGUI панелей.
/// Сам создаёт 8 точек через код — никаких дополнительных настроек в префабе не нужно.
/// Разместить: добавить на любой GameObject внутри Canvas,
/// позиционировать через RectTransform как обычный UI элемент.
/// </summary>
public class LoadingSpinnerUI : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private float duration = 1.1f;

    [Header("Точки")]
    [SerializeField] private float dotSize   = 10f;
    [SerializeField] private Color dotColor  = new Color(156f / 255f, 116f / 255f, 83f / 255f, 1f);

    [Header("Текст 'Загрузка' (назначь дочерний TMP_Text)")]
    [SerializeField] private TMP_Text loadingLabel;

    // ── внутренние поля ──────────────────────────────────────────────
    private Image[] _dots   = new Image[8];
    private float   _elapsed;
    private Sprite  _circleSprite;

    private static readonly float[] OpacityPattern =
        { 1.0f, 0.7f, 0.5f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };

    // Позиции точек вокруг центра (в пикселях, как в CSS box-shadow 1em=10px)
    private static readonly Vector2[] DotOffsets =
    {
        new( 0f,    26f),   // 12 ч
        new( 18f,   18f),   // ~1:30
        new( 25f,    0f),   // 3 ч
        new( 17.5f,-17.5f), // ~4:30
        new( 0f,   -25f),   // 6 ч
        new(-18f,  -18f),   // ~7:30
        new(-26f,    0f),   // 9 ч
        new(-18f,   18f),   // ~10:30
    };

    // ── Unity lifecycle ──────────────────────────────────────────────
    void Awake()
    {
        _circleSprite = CreateCircleSprite(32);
        CreateDots();
    }

    void OnEnable()
    {
        _elapsed = 0f;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= duration) _elapsed -= duration;

        float headPos = (_elapsed / duration) * 8f;

        for (int i = 0; i < 8; i++)
        {
            float behind  = (headPos - i + 8f) % 8f;
            float opacity = SampleOpacity(behind);

            Color c = dotColor;
            c.a = opacity;
            _dots[i].color = c;
        }
    }

    // ── Публичный API ────────────────────────────────────────────────
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── Вспомогательные методы ───────────────────────────────────────
    private void CreateDots()
    {
        for (int i = 0; i < 8; i++)
        {
            var go = new GameObject($"dot-{i}");
            go.transform.SetParent(transform, false);

            var rt         = go.AddComponent<RectTransform>();
            rt.sizeDelta   = new Vector2(dotSize, dotSize);
            rt.anchoredPosition = DotOffsets[i];

            var img  = go.AddComponent<Image>();
            img.sprite = _circleSprite;
            img.type   = Image.Type.Simple;

            Color c = dotColor;
            c.a = 0.2f;
            img.color = c;

            _dots[i] = img;
        }
    }

    private float SampleOpacity(float behind)
    {
        int   idx  = Mathf.FloorToInt(behind);
        float frac = behind - idx;
        float from = OpacityPattern[Mathf.Clamp(idx,     0, 7)];
        float to   = OpacityPattern[Mathf.Clamp(idx + 1, 0, 7)];
        return Mathf.Lerp(from, to, frac);
    }

    // Генерирует круглый спрайт прямо в памяти — никаких внешних ассетов
    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r  = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x - r + 0.5f;
            float dy   = y - r + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float a    = Mathf.Clamp01(r - dist); // мягкий край
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}