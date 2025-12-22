using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class ProgressAPIService : MonoBehaviour
{
    private const string BASE_URL = "http://localhost:4000/api/v1";
    
    private static ProgressAPIService _instance;
    public static ProgressAPIService Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("[ProgressAPI] Создаём singleton...");
                GameObject go = new GameObject("ProgressAPIService");
                _instance = go.AddComponent<ProgressAPIService>();
                DontDestroyOnLoad(go);
                Debug.Log("[ProgressAPI] Singleton создан!");
            }
            return _instance;
        }
    }

    [Serializable]
    public class LevelCompletionRequest
    {
        public string challengeId;
        public int stars;
        public int completionTime;
        public int failedAttempts;
        public int hintsUsed;
        public int codeLines;
    }

    [Serializable]
    public class LevelCompletionResponse
    {
        public LevelCompletionData data;
        public string message;
    }

    [Serializable]
    public class LevelCompletionData
    {
        public StatsData stats;
        public int xpGained;
        public Achievement[] achievements;
    }

    [Serializable]
    public class StatsData
    {
        public int xp;
        public int level;
        public int challengesSolved;
        public int totalStars;
        public int totalFailedAttempts;
        public int hintsUsed;
        public int totalPlaytimeSeconds;
    }

    [Serializable]
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public bool isNew;
    }

    // ⭐ ПОЛУЧЕНИЕ ТОКЕНА (с поддержкой разных источников)
    private string GetAuthToken()
    {
        // 1. Пробуем TokenManager (основной способ)
        if (TokenManager.Instance != null && !string.IsNullOrEmpty(TokenManager.Instance.firebaseIdToken))
        {
            Debug.Log("[ProgressAPI] Токен из TokenManager");
            return TokenManager.Instance.firebaseIdToken;
        }
        
        // 2. Fallback на PlayerPrefs (если есть)
        string token = PlayerPrefs.GetString("authToken", "");
        if (!string.IsNullOrEmpty(token))
        {
            Debug.Log("[ProgressAPI] Токен из PlayerPrefs");
            return token;
        }
        
        // 3. DEV MODE: тестовый токен для разработки
        #if UNITY_EDITOR
        string devToken = PlayerPrefs.GetString("DEV_TOKEN", "");
        if (!string.IsNullOrEmpty(devToken))
        {
            Debug.Log("[ProgressAPI] Токен из DEV_TOKEN");
            return devToken;
        }
        #endif
        
        Debug.LogWarning("[ProgressAPI] Токен не найден!");
        return "";
    }

    public IEnumerator SaveLevelCompletion(
        string challengeId,
        int stars,
        int completionTime,
        int failedAttempts,
        int hintsUsed,
        int codeLines,
        Action<LevelCompletionResponse> onSuccess = null,
        Action<string> onError = null)
    {
        Debug.Log($"[ProgressAPI] SaveLevelCompletion вызван: {challengeId}");
        
        // ⭐ ПОЛУЧАЕМ ТОКЕН
        string token = GetAuthToken();
        Debug.Log($"[ProgressAPI] Auth token: {(string.IsNullOrEmpty(token) ? "ОТСУТСТВУЕТ!" : "OK (" + token.Length + " символов)")}");
        
        if (string.IsNullOrEmpty(token))
        {
            string errorMsg = "Токен авторизации отсутствует! Установи токен в TokenManager или PlayerPrefs.";
            Debug.LogError($"[ProgressAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }

        string url = $"{BASE_URL}/gamification/challenge-solved";
        Debug.Log($"[ProgressAPI] URL: {url}");
        
        var requestData = new LevelCompletionRequest
        {
            challengeId = challengeId,
            stars = stars,
            completionTime = completionTime,
            failedAttempts = failedAttempts,
            hintsUsed = hintsUsed,
            codeLines = codeLines
        };
        
        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"[ProgressAPI] Отправляем JSON: {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {token}");
            
            Debug.Log("[ProgressAPI] Запускаем запрос...");
            yield return www.SendWebRequest();
            
            Debug.Log($"[ProgressAPI] Запрос завершён. Result: {www.result}");
            Debug.Log($"[ProgressAPI] Response Code: {www.responseCode}");
            Debug.Log($"[ProgressAPI] Response Text: {www.downloadHandler.text}");
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseJson = www.downloadHandler.text;
                Debug.Log($"[ProgressAPI] ✅ Успешно! Response: {responseJson}");
                
                try
                {
                    var response = JsonUtility.FromJson<LevelCompletionResponse>(responseJson);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ProgressAPI] ❌ Ошибка парсинга JSON: {e.Message}");
                    Debug.LogError($"Raw response: {responseJson}");
                    onError?.Invoke($"Ошибка парсинга: {e.Message}");
                }
            }
            else
            {
                string errorMsg = $"HTTP {www.responseCode}: {www.error}";
                Debug.LogError($"[ProgressAPI] ❌ Ошибка запроса: {errorMsg}");
                Debug.LogError($"Response text: {www.downloadHandler.text}");
                onError?.Invoke(errorMsg);
            }
        }
    }
}
