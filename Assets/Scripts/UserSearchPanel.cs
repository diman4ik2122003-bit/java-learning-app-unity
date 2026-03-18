using System.Collections;
using TMPro;
using UnityEngine;

public class UserSearchPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField discriminatorInput;
    [SerializeField] private GameObject     notFoundText;

    [Header("References")]
    [SerializeField] private BoardSlideSwitcher boardSlideSwitcher;
    [SerializeField] private ProfileManager     profileManager;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

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

    private IEnumerator SearchAndOpen(string nickname, string discriminator)
    {
        while (TokenManager.Instance == null || !TokenManager.Instance.IsSessionReady)
            yield return new WaitForSeconds(0.1f);

        TokenManager.FriendsResponse response = null;
        yield return TokenManager.Instance.SearchPeople(nickname, discriminator, r => response = r);

        if (response?.data == null || response.data.Length == 0)
        {
            if (debugLogs) Debug.Log("[UserSearchPanel] User not found");
            PopupManager.Instance?.Show("Пользователь не найден");
            yield break;
        }

        TokenManager.FriendData found = response.data[0];
        if (debugLogs) Debug.Log($"[UserSearchPanel] Found uid={found.uid}, opening profile");

        // Сохраняем данные в ProfileManager ДО открытия панели
        profileManager?.SetPendingData(found);

        if (boardSlideSwitcher != null)
            boardSlideSwitcher.ForceOpenProfile(found.uid);
        else
            Debug.LogWarning("[UserSearchPanel] BoardSlideSwitcher not set!");
    }

    private void SetNotFound(bool visible)
    {
        if (notFoundText != null)
            notFoundText.SetActive(visible);
    }
}
