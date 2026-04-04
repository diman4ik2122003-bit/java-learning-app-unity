using UnityEngine;
using UnityEngine.UIElements;

public class LoaderSpinnerAnimation : MonoBehaviour
{
    [Header("Ссылка на UIDocument этого лоадера")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Скорость (секунд на оборот)")]
    [SerializeField] private float duration = 1.1f;

    private VisualElement[] _dots = new VisualElement[8];
    private float _elapsed;

    // Яркость каждой точки: 0й элемент = голова (самая яркая), дальше затухает
    private static readonly float[] OpacityPattern =
        { 1.0f, 0.7f, 0.5f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };

    void Start()
    {
        var root = uiDocument.rootVisualElement;
        var loaderRoot = root.Q<VisualElement>("loader-root");

        if (loaderRoot == null)
        {
            Debug.LogError("[LoaderSpinner] Элемент 'loader-root' не найден в UXML!");
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            _dots[i] = loaderRoot.Q<VisualElement>($"dot-{i}");

            if (_dots[i] == null)
                Debug.LogWarning($"[LoaderSpinner] dot-{i} не найден!");
        }
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        // Зацикливаем таймер
        if (_elapsed >= duration)
            _elapsed -= duration;

        // Непрерывная позиция "головы" от 0 до 8
        float headPos = (_elapsed / duration) * 8f;

        for (int i = 0; i < 8; i++)
        {
            if (_dots[i] == null) continue;

            // Насколько эта точка отстаёт от головы
            float behind = (headPos - i + 8f) % 8f;

            _dots[i].style.opacity = SampleOpacity(behind);
        }
    }

    private float SampleOpacity(float behind)
    {
        int idx  = Mathf.FloorToInt(behind);
        float frac = behind - idx;

        float from = OpacityPattern[Mathf.Clamp(idx,     0, 7)];
        float to   = OpacityPattern[Mathf.Clamp(idx + 1, 0, 7)];

        return Mathf.Lerp(from, to, frac);
    }
}