using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SSEClient : MonoBehaviour
{
    public static SSEClient Instance { get; private set; }

    public event Action<string> OnFriendRequest;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private UnityWebRequest _request;
    private bool _connected = false;
    private string _token;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect(string token)
    {
        if (_connected)
        {
            if (debugLogs) Debug.Log("[SSEClient] Already connected, skipping");
            return;
        }
        _token = token;
        StartCoroutine(StreamCoroutine());
    }

    public void Disconnect()
    {
        _connected = false;
        _request?.Abort();
        _request = null;
        if (debugLogs) Debug.Log("[SSEClient] Disconnected");
    }

    private IEnumerator StreamCoroutine()
    {
        _connected = true;
        string url = TokenManager.Instance.ApiBaseUrl.TrimEnd('/') + "/notifications/stream";

        if (debugLogs) Debug.Log($"[SSEClient] Connecting to {url}");

        while (_connected)
        {
            var handler = new SSEDownloadHandler(line =>
            {
                if (debugLogs) Debug.Log($"[SSEClient] Line received: {line}");

                if (!line.StartsWith("data:")) return;

                string json = line.Substring(5).Trim();
                if (string.IsNullOrEmpty(json)) return;

                // Парсим тип события
                if (json.Contains("\"friend_request\""))
                {
                    if (debugLogs) Debug.Log("[SSEClient] Friend request event!");
                    OnFriendRequest?.Invoke(json);
                }
            });

            using (_request = new UnityWebRequest(url, "GET", handler, null))
            {
                _request.SetRequestHeader("Authorization", "Bearer " + _token);
                _request.SetRequestHeader("Accept", "text/event-stream");
                _request.SetRequestHeader("Cache-Control", "no-cache");
                _request.timeout = 0;

                yield return _request.SendWebRequest();

                if (!_connected) yield break;

                if (debugLogs) Debug.LogWarning($"[SSEClient] Stream ended ({_request.error}), reconnecting in 5s...");
            }

            _request = null;
            yield return new WaitForSeconds(5f);
        }
    }
}

// ─── Кастомный handler для построчного чтения стрима ───────────────────────

public class SSEDownloadHandler : DownloadHandlerScript
{
    private readonly Action<string> _onLine;
    private readonly StringBuilder _buffer = new StringBuilder();

    public SSEDownloadHandler(Action<string> onLine) : base(new byte[4096])
    {
        _onLine = onLine;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
        _buffer.Append(chunk);

        string buf = _buffer.ToString();
        int idx;
        while ((idx = buf.IndexOf('\n')) >= 0)
        {
            string line = buf.Substring(0, idx).TrimEnd('\r');
            buf = buf.Substring(idx + 1);
            if (!string.IsNullOrEmpty(line) && !line.StartsWith(":"))
                _onLine?.Invoke(line);
        }

        _buffer.Clear();
        _buffer.Append(buf);
        return true;
    }

    protected override void CompleteContent() { }
    protected override float GetProgress() => 0f;
}
