using UnityEngine;

public class LangTest : MonoBehaviour
{
    [ContextMenu("Lang RU")]
    void LangRu()
    {
        LocalizationManager.Instance.SetLanguage("ru");
    }

    [ContextMenu("Lang EN")]
    void LangEn()
    {
        LocalizationManager.Instance.SetLanguage("en");
    }
}
