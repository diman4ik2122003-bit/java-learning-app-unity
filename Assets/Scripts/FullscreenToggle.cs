using UnityEngine;
using UnityEngine.UI;

public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        toggle.isOn = Screen.fullScreen;
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
{
    Debug.Log("Toggle changed: " + value);
    Screen.fullScreen = value;
}


    private void Update()
{
    if (toggle.isOn != Screen.fullScreen)
    {
        Debug.Log("Sync toggle: " + Screen.fullScreen);
        toggle.isOn = Screen.fullScreen;
    }
}

}
