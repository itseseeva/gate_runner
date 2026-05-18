using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Панель снизу экрана с информацией о выделенном здании.
/// Появляется при OnBuildingSelected, скрывается при OnSelectionCleared.
///
/// Анимация — выезжает снизу через DOTween (как панель биома в MainMenu).
/// </summary>
public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject       _root;          // сам объект BuildingInfoPanel
    [SerializeField] private CanvasGroup      _canvasGroup;
    [SerializeField] private RectTransform    _rectTransform;
    [SerializeField] private TextMeshProUGUI  _nameText;
    [SerializeField] private TextMeshProUGUI  _levelText;
    [SerializeField] private TextMeshProUGUI  _productionText;
    [SerializeField] private TextMeshProUGUI  _costText;
    [SerializeField] private Button           _upgradeButton;
    [SerializeField] private Button           _closeButton;

    [Header("Анимация")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _hiddenY = -400f;  // ниже экрана

    private BuildingView _currentView;

    private void Start()
    {
        // Скрываем по умолчанию
        if (_root != null) _root.SetActive(false);

        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);

        BuildingSelector.OnBuildingSelected += Show;
        BuildingSelector.OnSelectionCleared += Hide;

        BaseManager.OnBuildingsChanged += OnBuildingsChanged;
    }

    private void OnDestroy()
    {
        BuildingSelector.OnBuildingSelected -= Show;
        BuildingSelector.OnSelectionCleared -= Hide;

        BaseManager.OnBuildingsChanged -= OnBuildingsChanged;

        if (_rectTransform != null) _rectTransform.DOKill();
        if (_canvasGroup != null) _canvasGroup.DOKill();
    }

    // ─── Показ / скрытие ────────────────────────────────────

    private void Show(BuildingView view)
    {
        if (view == null) return;
        _currentView = view;

        if (_root != null) _root.SetActive(true);

        RefreshInfo();

        // Анимация выезда снизу
        if (_rectTransform != null)
        {
            _rectTransform.DOKill();
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _hiddenY);
            _rectTransform.DOAnchorPosY(0f, _slideDuration).SetEase(Ease.OutQuad);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, _slideDuration);
        }
    }

    public void Hide()
    {
        _currentView = null;

        if (_rectTransform != null)
        {
            _rectTransform.DOKill();
            _rectTransform.DOAnchorPosY(_hiddenY, _slideDuration).SetEase(Ease.InQuad);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, _slideDuration).OnComplete(() =>
            {
                if (_root != null) _root.SetActive(false);
            });
        }
        else if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    // ─── Обновление инфы ────────────────────────────────────

    private void RefreshInfo()
    {
        if (_currentView == null) return;

        BuildingInstance inst = _currentView.Instance;
        BuildingDataSO   data = _currentView.Data;

        if (inst == null || data == null) return;

        // Имя
        if (_nameText != null)
            _nameText.text = data.DisplayName;

        // Уровень
        if (_levelText != null)
            _levelText.text = $"Level {inst.Level}";

        // Описание производства (для GoldMine/IronMine/Storage)
        if (_productionText != null)
            _productionText.text = GetProductionText(data, inst.Level);

        // Стоимость следующего уровня
        if (_costText != null)
            _costText.text = GetCostText(data, inst.Level);

        // Кнопка апгрейда — активна если есть ресурсы и не максимум
        UpdateUpgradeButton(data, inst);
    }

    private string GetProductionText(BuildingDataSO data, int level)
    {
        // Producer-здания
        if (data is ProducerBuildingDataSO producer)
        {
            int prod = producer.GetProduction(level);
            if (data.Type == BuildingType.GoldMine) return $"Production: +{prod} gold/sec";
            if (data.Type == BuildingType.IronMine) return $"Production: +{prod} iron/sec";
        }

        // Storage-здания
        if (data is StorageBuildingDataSO storage)
        {
            return $"Capacity: {storage.GetCapacity(level)}";
        }

        return data.Description;
    }

    private string GetCostText(BuildingDataSO data, int currentLevel)
    {
        // Стоимость СЛЕДУЮЩЕГО уровня
        int nextLevel = currentLevel + 1;
        if (nextLevel > data.MaxLevel)
            return "<color=#FF5E3A>MAX LEVEL</color>";

        var nextData = data.GetLevel(nextLevel);
        if (nextData == null) return "";

        // Собираем цену
        string parts = "";
        if (nextData.CostGold > 0) parts += $"<color=#F5C45E>{nextData.CostGold} gold</color> ";
        if (nextData.CostIron > 0) parts += $"<color=#5EC8FF>{nextData.CostIron} iron</color> ";

        if (string.IsNullOrEmpty(parts)) parts = "Free";

        string time = nextData.UpgradeTimeSeconds > 0
            ? $" · {FormatTime(nextData.UpgradeTimeSeconds)}"
            : "";

        return $"Cost: {parts}{time}";
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 60f) return $"{seconds:F0}s";
        if (seconds < 3600f) return $"{Mathf.FloorToInt(seconds / 60f)}m";
        return $"{Mathf.FloorToInt(seconds / 3600f)}h";
    }

    private void UpdateUpgradeButton(BuildingDataSO data, BuildingInstance inst)
    {
        if (_upgradeButton == null) return;

        // Достигли максимума?
        if (inst.Level >= data.MaxLevel)
        {
            _upgradeButton.interactable = false;
            return;
        }

        // Хватает ли ресурсов?
        var nextData = data.GetLevel(inst.Level + 1);
        if (nextData == null) { _upgradeButton.interactable = false; return; }

        bool canAfford = ResourceManager.Instance != null
            && ResourceManager.Instance.CanAfford(nextData.CostGold, nextData.CostIron);

        _upgradeButton.interactable = canAfford;
    }

    private void OnUpgradeClicked()
    {
        if (_currentView == null) return;

        bool started = BaseManager.Instance.StartUpgrade(_currentView.Instance.Id);
        if (started)
        {
            RefreshInfo();
        }
        else
        {
            Debug.LogWarning("[InfoPanel] Не удалось запустить апгрейд (нет ресурсов / уже идёт / макс)", this);
        }
    }

    private void OnBuildingsChanged()
    {
        // Если панель открыта — обновляем инфу
        if (_currentView != null && _root.activeSelf)
            RefreshInfo();
    }
}
