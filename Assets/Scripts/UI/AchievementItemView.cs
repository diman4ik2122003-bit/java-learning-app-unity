using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class AchievementItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Loading")]
    [SerializeField] private CanvasGroup loadingSpinner;

    public void Bind(string title, string description, string imageUrl, bool unlocked = true)
    {
        if (titleText)
            titleText.text = title ?? "";

        if (descriptionText)
            descriptionText.text = description ?? "";

        if (iconImage)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                StartCoroutine(LoadImageFromUrl(imageUrl));
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }

        Debug.Log($"[AchievementItemView] Bound: title='{title}', description='{description}', imageUrl='{imageUrl}', unlocked={unlocked}");
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        if (loadingSpinner)
            loadingSpinner.alpha = 1f;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (loadingSpinner)
                loadingSpinner.alpha = 0f;

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                if (texture != null && iconImage != null)
                {
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogError($"[AchievementItemView] Failed to load image from {url}: {uwr.error}");

                if (iconImage)
                    iconImage.color = new Color(1, 1, 1, 0.3f);
            }
        }
    }
}
