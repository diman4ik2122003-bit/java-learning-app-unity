using TMPro;
using UnityEngine;

public class StatsPanelBinder : MonoBehaviour
{
    [Header("TMP refs")]
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtXp;
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private TextMeshProUGUI txtStreakDays;
    [SerializeField] private TextMeshProUGUI txtCompletedLessons;
    [SerializeField] private TextMeshProUGUI txtCompletedCourses;
    [SerializeField] private TextMeshProUGUI txtAchievementsCount;
    [SerializeField] private TextMeshProUGUI txtTotalChallengesSolved;

    private TokenManager.UserProfileResponse _pendingProfile;
    private TokenManager.UserStatsResponse _pendingStats;
    private bool _hasPendingData = false;

    private void OnEnable()
    {
        if (_hasPendingData)
            RebuildUI();
    }

    public void Apply(TokenManager.UserProfileResponse profile, TokenManager.UserStatsResponse stats)
    {
        _pendingProfile = profile;
        _pendingStats = stats;
        _hasPendingData = true;

        if (!gameObject.activeInHierarchy) return;

        RebuildUI();
    }

    private void RebuildUI()
    {
        var p = _pendingProfile?.data;
        if (p == null) return;

        if (txtLevel) txtLevel.text = p.stats.level.ToString();
        if (txtXp) txtXp.text = p.stats.xp.ToString();
        if (txtCoins) txtCoins.text = p.gamification.coins.ToString();
        if (txtStreakDays) txtStreakDays.text = p.gamification.streakDays.ToString();
        if (txtCompletedLessons) txtCompletedLessons.text = p.stats.completedLessons.ToString();
        if (txtCompletedCourses) txtCompletedCourses.text = p.stats.completedCourses.ToString();
        if (txtAchievementsCount) txtAchievementsCount.text = p.stats.achievementsCount.ToString();

        var s = _pendingStats?.data;
        if (s != null && txtTotalChallengesSolved)
            txtTotalChallengesSolved.text = s.totalChallengesSolved.ToString();
    }
}
