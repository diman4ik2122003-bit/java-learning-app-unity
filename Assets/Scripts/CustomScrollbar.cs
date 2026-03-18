using UnityEngine;
using UnityEngine.UI;

public class CustomScrollbar : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Slider slider;
    public bool invert = true;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        // CanvasGroup для скрытия без отключения логики
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        slider.onValueChanged.AddListener(OnSliderChanged);

        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScrollRectChanged);
    }

    void Start()
    {
        // Проверяем сразу после инициализации layout'а
        UpdateVisibility();
    }

    void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderChanged);

        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScrollRectChanged);
    }

    void OnSliderChanged(float value)
    {
        if (scrollRect == null) return;
        scrollRect.verticalNormalizedPosition = invert ? 1f - value : value;
    }

    void OnScrollRectChanged(Vector2 pos)
    {
        if (slider == null) return;
        float v = invert ? 1f - pos.y : pos.y;
        slider.SetValueWithoutNotify(v);
        UpdateVisibility();
    }

    /// <summary>
    /// Вызывай вручную после добавления/удаления элементов в список.
    /// </summary>
    public void UpdateVisibility()
    {
        if (scrollRect == null || _canvasGroup == null) return;

        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.GetComponent<RectTransform>();

        RectTransform content = scrollRect.content;

        if (viewport == null || content == null) return;

        bool needsScroll = content.rect.height > viewport.rect.height;

        _canvasGroup.alpha          = needsScroll ? 1f : 0f;
        _canvasGroup.blocksRaycasts = needsScroll;
        _canvasGroup.interactable   = needsScroll;
    }
}
