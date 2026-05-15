using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Контроллер главного меню.
/// Отображает прогресс игрока (Gold/XP/Level) и обрабатывает выбор биома.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Верхний бар — прогресс")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _xpText;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Биомы")]
    [SerializeField] private Button       _forestButton;
    [SerializeField] private BiomeDataSO  _forestBiome;

    [Header("Desert")]
    [SerializeField] private Button       _desertButton;
    [SerializeField] private BiomeDataSO  _desertBiome;

    [Tooltip("Главное меню — скрывается при открытии BiomeView")]
    [SerializeField] private GameObject _mainMenuRoot;

    [Header("Биом view")]
    [SerializeField] private BiomeView _biomeView;

    [Header("Сцена геймплея")]
    [Tooltip("Имя сцены геймплея — должна совпадать с файлом в Build Profiles")]
    [SerializeField] private string _gameplaySceneName = "SampleScene";

    private void Start()
    {
        UpdateUI();

        // Подписываемся на изменения данных
        PlayerDataManager.OnDataChanged += UpdateUI;

        // Кнопка биома
        if (_forestButton != null)
            _forestButton.onClick.AddListener(OnForestClicked);

        if (_desertButton != null)
            _desertButton.onClick.AddListener(OnDesertClicked);
    }

    private void OnDestroy()
    {
        PlayerDataManager.OnDataChanged -= UpdateUI;
    }

    /// <summary>Обновляет верхний бар. Вызывается при изменении данных.</summary>
    private void UpdateUI()
    {
        if (PlayerDataManager.Instance == null) return;

        var p = PlayerDataManager.Instance;

        if (_goldText  != null) _goldText.text  = $"<sprite=0> {p.Gold}";
        if (_xpText    != null) _xpText.text    = $"<sprite=0> {p.XP} / {p.GetXPForNextLevel()}";
        if (_levelText != null) _levelText.text = $"<sprite=0> {p.AccountLevel}";
    }

    private void OnForestClicked()
    {
        if (_forestBiome == null)
        {
            Debug.LogError("[MainMenu] Forest Biome не задан!", this);
            return;
        }

        if (_biomeView == null)
        {
            Debug.LogError("[MainMenu] BiomeView не задан!", this);
            return;
        }

        if (_mainMenuRoot != null) _mainMenuRoot.SetActive(false);
        _biomeView.Show(_forestBiome);
    }

    private void OnDesertClicked()
    {
        if (_desertBiome == null)
        {
            Debug.LogError("[MainMenu] Desert Biome не задан!", this);
            return;
        }

        if (_biomeView == null)
        {
            Debug.LogError("[MainMenu] BiomeView не задан!", this);
            return;
        }

        if (_mainMenuRoot != null) _mainMenuRoot.SetActive(false);
        _biomeView.Show(_desertBiome);
    }

    public void ShowMainMenu()
    {
        if (_mainMenuRoot != null) _mainMenuRoot.SetActive(true);
    }
}
