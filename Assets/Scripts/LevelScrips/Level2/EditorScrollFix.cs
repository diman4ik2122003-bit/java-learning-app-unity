using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class EditorScrollFix : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField inputField;

    public void OnPointerClick(PointerEventData eventData)
    {
        // кликаем по области — фокусируем InputField
        if (inputField != null)
        {
            inputField.ActivateInputField();
            
            // устанавливаем каретку в позицию клика
            int charIndex = TMP_TextUtilities.FindIntersectingCharacter(
                inputField.textComponent,
                eventData.position,
                Camera.main,
                true
            );
            
            if (charIndex >= 0)
                inputField.caretPosition = charIndex;
        }
    }
}
