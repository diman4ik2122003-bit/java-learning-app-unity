using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategorySectionView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform itemsParent;
    
    [Header("Scroll Controls")]
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 0.3f;
    
    private RectTransform contentRect;
    private float itemWidth = 0f;
    private bool isScrolling = false;
    private Vector2 lastContentPosition;

    public Transform ItemsParent => itemsParent;

    private void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }

        if (scrollRect != null)
        {
            contentRect = scrollRect.content;
        }

        if (leftArrow != null)
        {
            leftArrow.onClick.AddListener(ScrollLeft);
        }

        if (rightArrow != null)
        {
            rightArrow.onClick.AddListener(ScrollRight);
        }
    }

    private void OnDestroy()
    {
        if (leftArrow != null)
        {
            leftArrow.onClick.RemoveListener(ScrollLeft);
        }

        if (rightArrow != null)
        {
            rightArrow.onClick.RemoveListener(ScrollRight);
        }
    }

    private void Start()
    {
        // Принудительно устанавливаем начальную позицию
        if (contentRect != null)
        {
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            lastContentPosition = contentRect.anchoredPosition;
        }
        
        CalculateItemWidth();
        UpdateArrowsState();
    }

    private void Update()
    {
        if (contentRect != null && contentRect.anchoredPosition != lastContentPosition)
        {
            lastContentPosition = contentRect.anchoredPosition;
            UpdateArrowsState();
        }
    }

    public void SetTitle(string title)
    {
        if (titleText) titleText.text = title;
        Debug.Log($"[CategorySectionView] Title set to: '{title}'");
    }

    private void CalculateItemWidth()
    {
        if (itemsParent == null || itemsParent.childCount == 0)
        {
            itemWidth = 200f;
            return;
        }

        RectTransform firstItem = itemsParent.GetChild(0) as RectTransform;
        if (firstItem != null)
        {
            itemWidth = firstItem.rect.width;

            HorizontalLayoutGroup layoutGroup = itemsParent.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                itemWidth += layoutGroup.spacing;
            }

            Debug.Log($"[CategorySectionView] Calculated item width: {itemWidth}");
        }
    }

    public void OnItemsAdded()
    {
        // Сбрасываем позицию при добавлении элементов
        if (contentRect != null)
        {
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            lastContentPosition = contentRect.anchoredPosition;
        }
        
        CalculateItemWidth();
        UpdateArrowsState();
    }

    private void ScrollLeft()
    {
        if (isScrolling || scrollRect == null || contentRect == null) return;

        if (itemWidth == 0f)
        {
            CalculateItemWidth();
        }

        StartCoroutine(SmoothScroll(itemWidth));
    }

    private void ScrollRight()
    {
        if (isScrolling || scrollRect == null || contentRect == null) return;

        if (itemWidth == 0f)
        {
            CalculateItemWidth();
        }

        StartCoroutine(SmoothScroll(-itemWidth));
    }

    private System.Collections.IEnumerator SmoothScroll(float offset)
    {
        isScrolling = true;

        float startPos = contentRect.anchoredPosition.x;
        float targetPos = startPos + offset;

        float minPos = 0f;
        float maxPos = Mathf.Max(0f, contentRect.rect.width - scrollRect.viewport.rect.width);
        targetPos = Mathf.Clamp(targetPos, -maxPos, minPos);

        float elapsed = 0f;

        while (elapsed < scrollSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scrollSpeed);
            
            t = 1f - (1f - t) * (1f - t);

            float newX = Mathf.Lerp(startPos, targetPos, t);
            contentRect.anchoredPosition = new Vector2(newX, contentRect.anchoredPosition.y);

            yield return null;
        }

        contentRect.anchoredPosition = new Vector2(targetPos, contentRect.anchoredPosition.y);
        lastContentPosition = contentRect.anchoredPosition;

        isScrolling = false;
        UpdateArrowsState();
    }

    private void UpdateArrowsState()
    {
        if (scrollRect == null || contentRect == null) return;

        float contentWidth = contentRect.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;
        float currentX = contentRect.anchoredPosition.x;

        bool canScroll = contentWidth > viewportWidth + 0.1f;

        if (!canScroll)
        {
            if (leftArrow != null)
            {
                leftArrow.gameObject.SetActive(true);
                leftArrow.interactable = false;
            }
            if (rightArrow != null)
            {
                rightArrow.gameObject.SetActive(true);
                rightArrow.interactable = false;
            }
            return;
        }

        if (leftArrow != null)
        {
            leftArrow.gameObject.SetActive(true);
            leftArrow.interactable = currentX < -0.1f;
        }

        if (rightArrow != null)
        {
            rightArrow.gameObject.SetActive(true);
            float maxScroll = contentWidth - viewportWidth;
            rightArrow.interactable = Mathf.Abs(currentX) < (maxScroll - 0.1f);
        }
    }

    public void RefreshScrollState()
    {
        CalculateItemWidth();
        UpdateArrowsState();
    }
}
