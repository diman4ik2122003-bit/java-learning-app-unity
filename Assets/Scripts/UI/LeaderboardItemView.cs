using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LeaderboardItemView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txtRank;
    [SerializeField] private TextMeshProUGUI txtDisplayName;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtXp;
    [SerializeField] private Image avatarImage;

    [Header("Highlight Settings")]
    [SerializeField] private TMP_FontAsset normalFont;
    [SerializeField] private TMP_FontAsset highlightFont;
    [SerializeField] private Color normalTextColor = new Color(222f/255f, 176f/255f, 120f/255f, 1f);
    [SerializeField] private Color highlightTextColor = new Color(222f/255f, 165f/255f, 60f/255f, 1f);

    [Header("Avatar Settings")]
    [SerializeField] private Sprite defaultAvatarSprite;

    private string _lastLoadedUrl;

    public void Bind(
        int rank,
        string displayName,
        string discriminator,
        int level,
        int xp,
        string photoURL,
        bool isCurrentUser = false)
    {
        if (txtRank) txtRank.text = $"#{rank}";
        if (txtDisplayName) txtDisplayName.text = displayName;
        if (txtLevel) txtLevel.text = $"Lvl {level}";
        if (txtXp) txtXp.text = $"{xp:N0} XP";

        TMP_FontAsset fontToUse = isCurrentUser ? highlightFont : normalFont;
        Color textColor = isCurrentUser ? highlightTextColor : normalTextColor;
        FontStyles fontStyle = isCurrentUser ? FontStyles.Bold : FontStyles.Normal;

        void ApplyStyle(TextMeshProUGUI t)
        {
            if (!t) return;
            if (fontToUse) t.font = fontToUse;
            t.color = textColor;
            t.fontStyle = fontStyle;
        }

        ApplyStyle(txtRank);
        ApplyStyle(txtDisplayName);
        ApplyStyle(txtLevel);
        ApplyStyle(txtXp);

        if (avatarImage)
        {
            if (!string.IsNullOrEmpty(photoURL) && photoURL != _lastLoadedUrl)
            {
                _lastLoadedUrl = photoURL;
                StartCoroutine(LoadAvatar(photoURL));
            }
            else if (string.IsNullOrEmpty(photoURL) && defaultAvatarSprite)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }
    }

    private IEnumerator LoadAvatar(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                if (defaultAvatarSprite && avatarImage)
                    avatarImage.sprite = defaultAvatarSprite;
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            if (texture == null || avatarImage == null) yield break;

            avatarImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            avatarImage.enabled = true;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
