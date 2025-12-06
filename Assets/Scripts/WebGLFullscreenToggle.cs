using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WebGLFullscreenToggle : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        // при старте синхронизируем галку
        toggle.isOn = Screen.fullScreen;
    }

    // клик по чекбоксу (нажатие мыши)
    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // инвертируем fullscreen прямо в обработчике ввода
        Screen.fullScreen = !Screen.fullScreen;
#endif
        // остальное пусть обрабатывает сам Toggle
    }

    private void Update()
    {
        // если fullscreen изменился извне (Esc) — подтянуть галку
        if (toggle.isOn != Screen.fullScreen)
            toggle.isOn = Screen.fullScreen;
    }
}
