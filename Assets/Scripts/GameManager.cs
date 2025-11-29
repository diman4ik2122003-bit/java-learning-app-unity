using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text nicknameText;

    void Start()
    {
        AudioManager.Instance.PlayMusic("MainMenuTheme");
    }

    public void SetNickname(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }
}
