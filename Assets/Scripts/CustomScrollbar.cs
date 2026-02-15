using UnityEngine;
using UnityEngine.UI;

public class CustomScrollbar : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Slider slider;
    public bool invert = true;

    void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        slider.onValueChanged.AddListener(OnSliderChanged);

        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScrollRectChanged);
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
        slider.SetValueWithoutNotify(v); // чтобы не вызывать OnSliderChanged рекурсивно
    }
}
