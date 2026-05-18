using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Визуальное представление одного здания на базе.
/// Висит на 3D-объекте куба-примитива.
///
/// Будет расширен в Дне 4 (выделение при тапе) и Дне 11 (иконки готовности).
/// </summary>
public class BuildingView : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private MeshRenderer    _meshRenderer;
    [SerializeField] private TextMeshPro     _labelText;
    [SerializeField] private Transform       _labelAnchor;  // где будет висеть надпись над зданием

    [Header("Прогресс апгрейда")]
    [SerializeField] private TextMeshPro _progressLabel;  // отдельный текст для прогресс-инфы

    [Header("Бафф")]
    [SerializeField] private GameObject _buffCanvas;    // World Space Canvas над зданием
    [SerializeField] private TextMeshProUGUI _buffText; // текст внутри canvas

    private BuildingInstance _instance;
    private BuildingDataSO   _data;

    private Vector3 _baseScale;
    private bool _isSelected;

    /// <summary>Настраивает здание данными из BaseManager.</summary>
    public void Setup(BuildingInstance instance, BuildingDataSO data)
    {
        _instance = instance;
        _data     = data;

        // Позиция
        transform.position = instance.Position;

        // Цвет и размер
        if (_meshRenderer != null && data != null)
        {
            // Создаём instance материала чтобы не менять общий
            var mat = new Material(_meshRenderer.material);
            mat.color = data.PrimitiveColor;
            _meshRenderer.material = mat;
        }

        // Высота примитива (масштаб по Y)
        if (data != null)
        {
            Vector3 scale = transform.localScale;
            scale.y = data.PrimitiveHeight;
            transform.localScale = scale;

            // Поднимаем здание чтобы оно стояло на земле, а не утопало
            Vector3 pos = transform.position;
            pos.y = data.PrimitiveHeight / 2f;
            transform.position = pos;

            // Запоминаем базовый scale для выделения
            _baseScale = transform.localScale;
        }

        RefreshLabel();
    }

    /// <summary>Обновляет надпись над зданием.</summary>
    public void RefreshLabel()
    {
        if (_labelText == null || _data == null || _instance == null) return;

        _labelText.text = $"{_data.DisplayName}\nLvl {_instance.Level}";
    }

    /// <summary>Возвращает данные здания.</summary>
    public BuildingInstance Instance => _instance;
    public BuildingDataSO   Data     => _data;

    /// <summary>Включает/выключает визуальное выделение здания (scale +5%).</summary>
    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;

        transform.DOKill();

        Vector3 targetScale = selected ? _baseScale * 1.05f : _baseScale;
        transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuad);
    }

    private void Update()
    {
        UpdateUpgradeProgress();
        UpdateBuffLabel();
    }

    private void UpdateUpgradeProgress()
    {
        if (_instance == null || _progressLabel == null) return;

        if (_instance.IsUpgrading)
        {
            var remaining = _instance.UpgradeEndTime - System.DateTime.UtcNow;
            if (remaining.TotalSeconds > 0)
            {
                _progressLabel.gameObject.SetActive(true);
                _progressLabel.text = $"⚙ Lvl {_instance.Level + 1}\n{FormatTime((float)remaining.TotalSeconds)}";
            }
            else
            {
                _progressLabel.gameObject.SetActive(false);
            }
        }
        else
        {
            if (_progressLabel.gameObject.activeSelf)
                _progressLabel.gameObject.SetActive(false);
        }
    }

    private void UpdateBuffLabel()
    {
        if (_buffCanvas == null || _buffText == null) return;
        if (ResourceManager.Instance == null) return;

        // Бафф показываем только на Producer-зданиях (GoldMine, IronMine)
        bool isProducer = _data is ProducerBuildingDataSO;
        bool active = isProducer && ResourceManager.Instance.IsBuffActive;

        if (_buffCanvas.activeSelf != active)
            _buffCanvas.SetActive(active);

        if (active)
        {
            float remaining = ResourceManager.Instance.BuffRemainingSeconds;
            _buffText.text = $"⚡ x{ResourceManager.Instance.BuffMultiplier:F0} · {FormatTime(remaining)}";
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 60f) return $"{seconds:F0}s";
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min}:{sec:00}";
    }
}
