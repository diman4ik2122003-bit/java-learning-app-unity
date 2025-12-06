using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WebGLFullscreenButton : Button
{
    public override void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Screen.fullScreen = !Screen.fullScreen;
#endif
        base.OnPointerDown(eventData);
    }
}
