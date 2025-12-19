using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardSlideSwitcher : MonoBehaviour
{
    public enum Tab { Stats, Achievements }

    [Header("Animate THIS (parent board panel)")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private CanvasGroup boardCanvasGroup; // optional

    [Header("Positions (anchoredPosition)")]
    [SerializeField] private Vector2 openAnchoredPos;
    [SerializeField] private Vector2 closedAnchoredPos;

    [Header("Content")]
    [SerializeField] private GameObject statsContent;
    [SerializeField] private GameObject achievementsContent;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float moveDownTime = 1.0f;
    [Min(0f)] [SerializeField] private float moveUpTime = 1.0f;

    [Header("Pause at top (only when switching tab)")]
    [Min(0f)] [SerializeField] private float topPauseSeconds = 0.25f;

    [Header("Swing after opening")]
    [Min(0f)] [SerializeField] private float swingAmplitude = 12f;
    [Min(0f)] [SerializeField] private float swingDuration = 0.85f;
    [Min(0)]  [SerializeField] private int swingOscillations = 1;

    [Header("Fade (optional)")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaWhenClosed = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine _routine;
    private bool _isOpen = false;
    [SerializeField] private Tab _currentTab = Tab.Stats;

    private void Reset()
    {
        boardRect = GetComponent<RectTransform>();
        boardCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!boardRect) boardRect = GetComponent<RectTransform>();

        boardRect.anchoredPosition = closedAnchoredPos;
        _isOpen = false;

        SetTabImmediate(_currentTab);

        if (boardCanvasGroup) boardCanvasGroup.alpha = alphaWhenClosed;

        if (debugLogs)
            Debug.Log($"[BoardSlideSwitcher] Awake open={openAnchoredPos} closed={closedAnchoredPos}", this);
    }

    public void OnStatsClicked() => HandleTabClick(Tab.Stats);
    public void OnAchievementsClicked() => HandleTabClick(Tab.Achievements);

    private void HandleTabClick(Tab clickedTab)
    {
        if (_routine != null) return;

        if (!statsContent || !achievementsContent || !boardRect)
        {
            Debug.LogError("[BoardSlideSwitcher] Not assigned: boardRect/statsContent/achievementsContent", this);
            return;
        }

        // closed -> open with requested tab
        if (!_isOpen)
        {
            _currentTab = clickedTab;
            SetTabImmediate(_currentTab);

            if (TokenManager.Instance != null)
                TokenManager.Instance.RefreshAll();

            _routine = StartCoroutine(OpenRoutine());
            return;
        }

        // open + click same tab -> close
        if (_isOpen && _currentTab == clickedTab)
        {
            _routine = StartCoroutine(CloseRoutine());
            return;
        }

        // open + click other tab -> switch
        _routine = StartCoroutine(SwitchTabRoutine(clickedTab));
    }

    private IEnumerator OpenRoutine()
    {
        yield return Move(boardRect.anchoredPosition, openAnchoredPos, moveDownTime, 1f, EaseOutCubic);
        _isOpen = true;

        yield return Swing(openAnchoredPos);

        _routine = null;
    }

    private IEnumerator CloseRoutine()
    {
        yield return Move(boardRect.anchoredPosition, closedAnchoredPos, moveUpTime, alphaWhenClosed, EaseInCubic);
        _isOpen = false;

        _routine = null;
    }

    private IEnumerator SwitchTabRoutine(Tab nextTab)
    {
        yield return Move(boardRect.anchoredPosition, closedAnchoredPos, moveUpTime, alphaWhenClosed, EaseInCubic);
        _isOpen = false;

        if (topPauseSeconds > 0f)
            yield return new WaitForSecondsRealtime(topPauseSeconds);

        _currentTab = nextTab;
        SetTabImmediate(_currentTab);
        yield return null;

        yield return Move(boardRect.anchoredPosition, openAnchoredPos, moveDownTime, 1f, EaseOutCubic);
        _isOpen = true;

        yield return Swing(openAnchoredPos);

        _routine = null;
    }

    private void SetTabImmediate(Tab tab)
    {
        bool showStats = tab == Tab.Stats;
        statsContent.SetActive(showStats);
        achievementsContent.SetActive(!showStats);

        if (debugLogs) Debug.Log($"[BoardSlideSwitcher] Tab={tab}", this);
    }

    private IEnumerator Move(Vector2 from, Vector2 to, float duration, float alphaTo, Func<float, float> ease)
    {
        if (duration <= 0.0001f)
        {
            boardRect.anchoredPosition = to;
            if (boardCanvasGroup) boardCanvasGroup.alpha = alphaTo;
            yield break;
        }

        float t = 0f;
        float startAlpha = boardCanvasGroup ? boardCanvasGroup.alpha : 1f;
        float targetAlpha = boardCanvasGroup ? alphaTo : 1f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float s = ease != null ? ease(p) : p;

            boardRect.anchoredPosition = Vector2.LerpUnclamped(from, to, s);

            if (boardCanvasGroup)
                boardCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, s);

            yield return null;
        }

        boardRect.anchoredPosition = to;
        if (boardCanvasGroup) boardCanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator Swing(Vector2 basePos)
    {
        if (swingAmplitude <= 0f || swingDuration <= 0f || swingOscillations <= 0)
            yield break;

        float t = 0f;
        while (t < swingDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / swingDuration);

            float damp = 1f - p;
            float phase = p * Mathf.PI * 2f * swingOscillations;
            float y = Mathf.Sin(phase) * swingAmplitude * damp;

            boardRect.anchoredPosition = basePos + new Vector2(0f, y);
            yield return null;
        }

        boardRect.anchoredPosition = basePos;
    }

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private float EaseInCubic(float x) => x * x * x;
}
