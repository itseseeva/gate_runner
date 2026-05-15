using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Панель биома — показывает заголовок биома и список уровней (кнопок).
/// Открывается из MainMenu при клике на биом.
/// При клике на уровень — запускает Gameplay.
/// </summary>
public class BiomeView : MonoBehaviour
{
    [Header("Визуал")]
    [SerializeField] private GameObject       _panel;
    [SerializeField] private TextMeshProUGUI  _biomeTitle;
    [SerializeField] private Transform        _levelsContainer;
    [SerializeField] private GameObject       _levelButtonPrefab;
    [SerializeField] private Button           _closeButton;

    [Header("Сцена геймплея")]
    [SerializeField] private string _gameplaySceneName = "SampleScene";

    private BiomeDataSO _currentBiome;

    private void Start()
    {
        if (_panel != null) _panel.SetActive(false);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    /// <summary>Открывает панель и заполняет её уровнями биома.</summary>
    public void Show(BiomeDataSO biome)
    {
        if (biome == null) return;
        _currentBiome = biome;

        if (_panel != null)
        {
            _panel.SetActive(true);

            // Анимация — выезжаем снизу + появляемся
            var rt = _panel.GetComponent<RectTransform>();
            var canvasGroup = _panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _panel.AddComponent<CanvasGroup>();

            rt.anchoredPosition = new Vector2(0, -Screen.height);
            canvasGroup.alpha = 0;

            rt.DOAnchorPosY(0, 0.4f).SetEase(Ease.OutQuad);
            canvasGroup.DOFade(1f, 0.3f);
        }

        if (_biomeTitle != null) _biomeTitle.text = biome.DisplayName;
        RefreshLevels();
    }

    public void Hide()
    {
        if (_panel != null) _panel.SetActive(false);

        // Показываем главное меню обратно
        MainMenuController main = FindAnyObjectByType<MainMenuController>();
        if (main != null) main.ShowMainMenu();
    }

    /// <summary>Перестраивает список кнопок уровней.</summary>
    private void RefreshLevels()
    {
        if (_currentBiome == null || _levelsContainer == null || _levelButtonPrefab == null) return;

        // Чистим старые кнопки
        foreach (Transform child in _levelsContainer)
            Destroy(child.gameObject);

        // Создаём кнопки для каждого уровня
        for (int i = 0; i < _currentBiome.LevelCount; i++)
        {
            GameObject btnGO = Instantiate(_levelButtonPrefab, _levelsContainer);
            LevelButton btn  = btnGO.GetComponent<LevelButton>();
            if (btn != null)
                btn.Setup(_currentBiome, i, this);
        }
    }

    /// <summary>Вызывается кнопкой уровня при клике.</summary>
    /// <summary>Вызывается кнопкой уровня при клике.</summary>
    public void OnLevelClicked(BiomeDataSO biome, int levelIndex)
    {
        if (biome == null || biome.LevelTemplate == null)
        {
            Debug.LogError("[BiomeView] Биом или шаблон уровня не задан!", this);
            return;
        }

        if (LevelLauncher.Instance == null)
        {
            Debug.LogError("[BiomeView] LevelLauncher не найден!", this);
            return;
        }

        LevelLauncher.Instance.SelectLevel(biome, levelIndex);
        SceneManager.LoadScene(_gameplaySceneName);
    }
}
