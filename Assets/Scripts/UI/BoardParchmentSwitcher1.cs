using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardSlideSwitcher : MonoBehaviour
{
    public enum Tab { Stats, Achievements, Profile, Leaderboard }

    [Header("Animate THIS (parent board panel)")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private CanvasGroup boardCanvasGroup;

    [Header("Positions (anchoredPosition)")]
    [SerializeField] private Vector2 openAnchoredPos;
    [SerializeField] private Vector2 closedAnchoredPos;

    [Header("Content")]
    [SerializeField] private GameObject statsContent;
    [SerializeField] private GameObject achievementsContent;
    [SerializeField] private GameObject profileContent;
    [SerializeField] private GameObject leaderboardContent;

    [Header("Data Title")]
    [SerializeField] private LocalizedText dataTitleLocalizedText;

    [Header("Localization Keys for Title")]
    [SerializeField] private string statsKey        = "data_stats_title";
    [SerializeField] private string achievementsKey = "data_achievements_title";
    [SerializeField] private string profileKey      = "data_profile_title";
    [SerializeField] private string leaderboardKey  = "data_leaderboard_title";

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float moveDownTime = 1.0f;
    [Min(0f)] [SerializeField] private float moveUpTime   = 1.0f;

    [Header("Pause at top (only when switching tab)")]
    [Min(0f)] [SerializeField] private float topPauseSeconds = 0.25f;

    [Header("Swing after opening")]
    [Min(0f)] [SerializeField] private float swingAmplitude    = 12f;
    [Min(0f)] [SerializeField] private float swingDuration     = 0.85f;
    [Min(0)]  [SerializeField] private int   swingOscillations = 1;

    [Header("Fade (optional)")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaWhenClosed = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine _routine;
    private bool _isOpen = false;
    [SerializeField] private Tab _currentTab = Tab.Stats;
    private string _pendingFriendUid = null;

    private void Reset()
    {
        boardRect        = GetComponent<RectTransform>();
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

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ВКЛАДОК ==========

    public void OnStatsClicked()        => HandleTabClick(Tab.Stats);
    public void OnAchievementsClicked() => HandleTabClick(Tab.Achievements);
    public void OnProfileClicked()      => HandleTabClick(Tab.Profile);
    public void OnLeaderboardClicked()  => HandleTabClick(Tab.Leaderboard);

    public void ForceOpenProfile(string friendUid)
    {
        if (debugLogs)
            Debug.Log($"[BoardSlideSwitcher] ForceOpenProfile uid={friendUid}, _isOpen={_isOpen}, _currentTab={_currentTab}");

        _pendingFriendUid = friendUid;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _routine = StartCoroutine(ForceOpenProfileRoutine());
    }

    private IEnumerator ForceOpenProfileRoutine()
    {
        if (debugLogs)
            Debug.Log($"[BoardSlideSwitcher] ForceOpenProfileRoutine START, _isOpen={_isOpen}");

        // Сохраняем uid локально сразу — чтобы повторный вызов не затёр _pendingFriendUid
        string uidToLoad = _pendingFriendUid;
        _pendingFriendUid = null;

        ProfileManager profileManager = FindFirstObjectByType<ProfileManager>(FindObjectsInactive.Include);

        if (profileManager == null)
            Debug.LogWarning("[BoardSlideSwitcher] ProfileManager not found!");

        // Если панель открыта — закрываем с анимацией
        if (_isOpen)
        {
            yield return Move(boardRect.anchoredPosition, closedAnchoredPos, moveUpTime, alphaWhenClosed, EaseInCubic);
            _isOpen = false;

            if (topPauseSeconds > 0f)
                yield return new WaitForSecondsRealtime(topPauseSeconds);
        }

        if (profileManager != null)
        {
            if (profileManager.gameObject.activeInHierarchy)
            {
                // profileContent уже активен (панель была на вкладке Profile)
                // OnEnable не сработает — грузим напрямую, минуя guard
                if (debugLogs) Debug.Log($"[BoardSlideSwitcher] ProfileManager active → ForceLoadProfile({uidToLoad})");
                profileManager.ForceLoadProfile(uidToLoad);
            }
            else
            {
                // profileContent неактивен — ставим uid в очередь для OnEnable
                if (debugLogs) Debug.Log($"[BoardSlideSwitcher] ProfileManager inactive → queuing LoadProfile({uidToLoad})");
                profileManager.ResetCurrentProfile();
                profileManager.LoadProfile(uidToLoad);
            }
        }

        _currentTab = Tab.Profile;
        SetTabImmediate(_currentTab); // активирует profileContent → OnEnable если был неактивен
        yield return null;

        yield return Move(boardRect.anchoredPosition, openAnchoredPos, moveDownTime, 1f, EaseOutCubic);
        _isOpen = true;

        yield return Swing(openAnchoredPos);

        _routine = null;

        if (debugLogs)
            Debug.Log("[BoardSlideSwitcher] ForceOpenProfileRoutine DONE");
    }

#if UNITY_EDITOR
    [ContextMenu("TEST ForceOpenProfile")]
    private void TestForceOpen()
    {
        Debug.Log($"[BSS] TEST CALL: _isOpen={_isOpen}, _routine={_routine != null}, pos={boardRect.anchoredPosition}");
        ForceOpenProfile("test_uid");
    }
#endif

    // ========== СТАНДАРТНАЯ ЛОГИКА ==========

    private void HandleTabClick(Tab clickedTab)
    {
        if (_routine != null) return;

        if (!statsContent || !achievementsContent || !boardRect)
        {
            Debug.LogError("[BoardSlideSwitcher] Not assigned: boardRect/statsContent/achievementsContent", this);
            return;
        }

        if (!_isOpen)
        {
            _currentTab = clickedTab;
            SetTabImmediate(_currentTab);

            if (TokenManager.Instance != null)
                TokenManager.Instance.RefreshAll();

            _routine = StartCoroutine(OpenRoutine());
            return;
        }

        if (_isOpen && _currentTab == clickedTab)
        {
            _routine = StartCoroutine(CloseRoutine());
            return;
        }

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

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    private void SetTabImmediate(Tab tab)
    {
        statsContent.SetActive(tab == Tab.Stats);
        achievementsContent.SetActive(tab == Tab.Achievements);
        profileContent.SetActive(tab == Tab.Profile);
        leaderboardContent.SetActive(tab == Tab.Leaderboard);

        UpdateDataTitle(tab);

        if (debugLogs) Debug.Log($"[BoardSlideSwitcher] Tab={tab}", this);
    }

    private void UpdateDataTitle(Tab tab)
    {
        if (dataTitleLocalizedText == null) return;

        string key = tab switch
        {
            Tab.Stats        => statsKey,
            Tab.Achievements => achievementsKey,
            Tab.Profile      => profileKey,
            Tab.Leaderboard  => leaderboardKey,
            _                => statsKey
        };

        dataTitleLocalizedText.key = key;

        if (LocalizationManager.Instance != null)
            dataTitleLocalizedText.UpdateText(LocalizationManager.Instance.CurrentLang);

        if (debugLogs)
            Debug.Log($"[BoardSlideSwitcher] Data title key changed to: {dataTitleLocalizedText.key}");
    }

    private IEnumerator Move(Vector2 from, Vector2 to, float duration, float alphaTo, Func<float, float> ease)
    {
        if (duration <= 0.0001f)
        {
            boardRect.anchoredPosition = to;
            if (boardCanvasGroup) boardCanvasGroup.alpha = alphaTo;
            yield break;
        }

        float t           = 0f;
        float startAlpha  = boardCanvasGroup ? boardCanvasGroup.alpha : 1f;
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

            float damp  = 1f - p;
            float phase = p * Mathf.PI * 2f * swingOscillations;
            float y     = Mathf.Sin(phase) * swingAmplitude * damp;

            boardRect.anchoredPosition = basePos + new Vector2(0f, y);
            yield return null;
        }

        boardRect.anchoredPosition = basePos;
    }

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private float EaseInCubic(float x)  => x * x * x;
}
