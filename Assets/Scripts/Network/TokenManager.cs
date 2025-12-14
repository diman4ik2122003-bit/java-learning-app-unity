// Assets/Scripts/Network/TokenManager.cs
using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class TokenManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterMessageListener();
#endif

    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private string backendUrl =
        "https://java-learning-app.onrender.com/api/v1/auth/profile";

    [HideInInspector]
    public string firebaseIdToken;

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RegisterMessageListener();
#endif
    }

    // ===== токен из JS =====
    public void ReceiveTokenFromJS(string token)
    {
        firebaseIdToken = token;
        
        // Передаём токен в JavaJudgeClient
        if (JavaJudgeClient.Instance != null)
        {
            JavaJudgeClient.Instance.SetAuthToken(token);
        }
        
        StartCoroutine(RequestProfile());
    }

    private IEnumerator RequestProfile()
    {
        UnityWebRequest www = UnityWebRequest.Get(backendUrl);
        www.SetRequestHeader("Authorization", "Bearer " + firebaseIdToken);
        yield return www.SendWebRequest();

        Debug.Log("HTTP response code: " + www.responseCode);
        Debug.Log("Ответ от сервера: " + www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            try
            {
                UserProfileResponse response =
                    JsonUtility.FromJson<UserProfileResponse>(json);
                Debug.Log("Получен никнейм: " + response.data.displayName);
                if (nicknameText)
                    nicknameText.text = response.data.displayName ?? "Нет ника";
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Ошибка разбора JSON: " + ex.Message);
                if (nicknameText)
                    nicknameText.text = "Ошибка данных";
            }
        }
        else
        {
            Debug.LogError("Ошибка запроса: " + www.error);
            if (!nicknameText) yield break;

            if (www.responseCode == 0)
                nicknameText.text = "Нет подключения";
            else
                nicknameText.text = $"Ошибка ({www.responseCode})";
        }
    }

    [System.Serializable]
    public class UserProfileData
    {
        public string uid;
        public string email;
        public string displayName;
        public string photoURL;
        public string role;
    }

    [System.Serializable]
    public class UserProfileResponse
    {
        public UserProfileData data;
    }
}
