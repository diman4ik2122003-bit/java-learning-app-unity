using UnityEngine;

public class SettingsToggle : MonoBehaviour
{
    public GameObject settingsPanel;

    public void TogglePanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}