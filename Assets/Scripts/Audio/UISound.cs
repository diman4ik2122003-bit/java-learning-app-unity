using UnityEngine;
using UnityEngine.UI;

public class UISound : MonoBehaviour
{
    public string soundName = "Wood";
    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn)
            btn.onClick.AddListener(() => AudioManager.Instance.PlaySFX(soundName));
    }
}
