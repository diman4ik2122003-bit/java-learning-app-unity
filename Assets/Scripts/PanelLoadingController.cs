using UnityEngine;

/// <summary>
/// Вешается на каждую панель (Stats Content, Achivs Content и т.д.).
/// Из кода загрузки данных вызывай StartLoading() / StopLoading().
/// </summary>
public class PanelLoadingController : MonoBehaviour
{
    [Header("Спиннер этой панели")]
    [SerializeField] private LoadingSpinnerUI spinner;

    [Header("Контент который прятать во время загрузки (опционально)")]
    [SerializeField] private GameObject[] contentToHide;

    private bool _isLoading;
    private bool _dataAlreadyLoaded;

    public bool IsLoading => _isLoading;

    private void Start()
    {
        // Запускаем спиннер только если данные ещё не пришли.
        // Если StopLoading() уже вызвали до Start() (биндер получил данные
        // раньше, чем Unity вызвал Start), не трогаем состояние.
        if (!_dataAlreadyLoaded)
            StartLoading();
    }

    /// <summary>Показать спиннер, скрыть контент.</summary>
    public void StartLoading()
    {
        _isLoading = true;

        if (spinner != null)
            spinner.Show();

        foreach (var obj in contentToHide)
            if (obj != null) obj.SetActive(false);
    }

    /// <summary>Скрыть спиннер, показать контент.</summary>
    public void StopLoading()
    {
        _dataAlreadyLoaded = true;  // ← запрещаем Start() снова включить спиннер
        _isLoading = false;

        if (spinner != null)
            spinner.Hide();

        foreach (var obj in contentToHide)
            if (obj != null) obj.SetActive(true);
    }
}