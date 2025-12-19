using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Icons (set in prefab)")]
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite unlockedIcon;

    public void Bind(string title, string description, bool unlocked)
    {
        if (titleText) titleText.text = title ?? "";
        if (descriptionText) descriptionText.text = description ?? "";

        if (iconImage)
            iconImage.sprite = unlocked ? unlockedIcon : lockedIcon;
    }
}
