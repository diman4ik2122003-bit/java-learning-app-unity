using System.Collections;
using TMPro;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float fadeInTime    = 0.3f;
    [SerializeField] private float holdTime      = 2.5f;
    [SerializeField] private float fadeOutTime   = 0.5f;

    private Coroutine _routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var c = messageText.color;
        messageText.color = new Color(c.r, c.g, c.b, 0f);
    }

    public void Show(string message)
    {
        messageText.text = message;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // Fade in
        yield return Fade(0f, 1f, fadeInTime);

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        yield return Fade(1f, 0f, fadeOutTime);

        _routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        Color c = messageText.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            messageText.color = c;
            yield return null;
        }

        c.a = to;
        messageText.color = c;
    }
}
