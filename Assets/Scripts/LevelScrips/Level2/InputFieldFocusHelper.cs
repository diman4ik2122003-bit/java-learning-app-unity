using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InputFieldFocusHelper : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField inputField;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputField != null)
        {
            // активируем InputField
            inputField.ActivateInputField();

            // пытаемся поставить каретку в позицию клика
            if (inputField.textComponent != null)
            {
                int charIndex = TMP_TextUtilities.FindIntersectingCharacter(
                    inputField.textComponent,
                    eventData.position,
                    null, // для Screen Space - Overlay используем null
                    true
                );

                if (charIndex >= 0 && charIndex <= inputField.text.Length)
                {
                    inputField.caretPosition = charIndex;
                    inputField.stringPosition = charIndex;
                }
            }
        }
    }
}
