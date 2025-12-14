// Assets/Scripts/Level/JavaJudgeClient.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class JavaJudgeClient : MonoBehaviour
{
    public static JavaJudgeClient Instance;

    [Header("API Settings")]
    public string judgeApiUrl = "https://java-learning-app.onrender.com/api/v1/judge/execute";
    public float timeout = 15f;

    private string authToken;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetAuthToken(string token)
    {
        authToken = token;
        Debug.Log("[JavaJudge] Auth token set");
    }

    public IEnumerator ExecuteJavaCode(string code, System.Action<JudgeResponse> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("[JavaJudge] No authentication token");
            onError?.Invoke("Нет токена авторизации");
            yield break;
        }

        JudgeRequest request = new JudgeRequest
        {
            code = code,
            language = "java",
            timeLimit = 5000, // ms
            memoryLimit = 256 // MB
        };

        string jsonData = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(judgeApiUrl, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + authToken);
        www.timeout = (int)timeout;

        Debug.Log("[JavaJudge] Sending code to judge API...");
        Debug.Log("[JavaJudge] URL: " + judgeApiUrl);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;
            Debug.Log("[JavaJudge] Response received: " + responseText);

            try
            {
                JudgeResponse response = JsonUtility.FromJson<JudgeResponse>(responseText);
                onSuccess?.Invoke(response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[JavaJudge] Parse error: " + ex.Message);
                Debug.LogError("[JavaJudge] Response text: " + responseText);
                onError?.Invoke("Ошибка разбора ответа сервера");
            }
        }
        else
        {
            string errorMsg = www.error;
            long responseCode = www.responseCode;
            string responseBody = www.downloadHandler.text;

            Debug.LogError($"[JavaJudge] Request failed: {errorMsg}");
            Debug.LogError($"[JavaJudge] Response code: {responseCode}");
            Debug.LogError($"[JavaJudge] Response body: {responseBody}");

            // Пытаемся распарсить ошибку от API
            try
            {
                ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseBody);
                onError?.Invoke($"Ошибка сервера: {errorResponse.message}");
            }
            catch
            {
                onError?.Invoke($"Ошибка запроса ({responseCode}): {errorMsg}");
            }
        }
    }
}

[System.Serializable]
public class JudgeRequest
{
    public string code;
    public string language;
    public int timeLimit;
    public int memoryLimit;
}

[System.Serializable]
public class JudgeResponse
{
    public bool success;
    public string output;
    public string error;
    public string compilationError;
    public float executionTime;
    public int exitCode;
}

[System.Serializable]
public class ErrorResponse
{
    public string message;
    public int statusCode;
}
