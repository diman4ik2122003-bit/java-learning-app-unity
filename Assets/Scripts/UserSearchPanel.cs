using System.Collections;
using TMPro;
using UnityEngine;

public class UserSearchPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField discriminatorInput;
    [SerializeField] private GameObject     notFoundText;

    [Header("Board Switcher")]
    [SerializeField] private BoardSlideSwitcher boardSlideSwitcher;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // ========== ПУБЛИЧНЫЙ МЕТОД — вешаем на кнопку ==========

    public void OnSearchButtonClicked()
    {
        string nickname      = nicknameInput?.text?.Trim() ?? "";
        string discriminator = discriminatorInput?.text?.Trim() ?? "";

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(discriminator))
        {
            if (debugLogs) Debug.Log("[UserSearchPanel] Empty fields, search cancelled");
            return;
        }

        if (debugLogs) Debug.Log($"[UserSearchPanel] Searching: {nickname}#{discriminator}");

        SetNotFound(false);
        StartCoroutine(SearchAndOpen(nickname, discriminator));
    }

    // ========== КОРУТИНА ПОИСКА ==========

    private IEnumerator SearchAndOpen(string nickname, string discriminator)
    {
        while (TokenManager.Instance == null || !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        TokenManager.FriendsResponse response = null;
        yield return TokenManager.Instance.SearchPeople(nickname, discriminator, r => response = r);

        if (response?.data == null || response.data.Length == 0)
        {
            if (debugLogs) Debug.Log("[UserSearchPanel] User not found");
            SetNotFound(true);
            yield break;
        }

        string uid = response.data[0].uid;
        if (debugLogs) Debug.Log($"[UserSearchPanel] Found uid={uid}, opening profile");

        if (boardSlideSwitcher != null)
            boardSlideSwitcher.ForceOpenProfile(uid);
        else
            Debug.LogWarning("[UserSearchPanel] BoardSlideSwitcher not set!");
    }

    // ========== ХЕЛПЕР ==========

    private void SetNotFound(bool visible)
    {
        if (notFoundText != null)
            notFoundText.SetActive(visible);
    }
}
