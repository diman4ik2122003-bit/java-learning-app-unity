using TMPro;
using UnityEngine;

public class CategorySectionView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform itemsParent; // Row/HorizontalScroll/Viewport/Content

    public Transform ItemsParent => itemsParent;

    public void SetTitle(string title)
    {
        if (titleText) titleText.text = title;
    }
}
