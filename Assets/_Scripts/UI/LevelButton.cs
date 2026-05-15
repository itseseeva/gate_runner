using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кнопка уровня в меню биома. Состояния: пройден ✓ / доступен ▶ / закрыт 🔒.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Header("Визуал")]
    [SerializeField] private Button           _button;
    [SerializeField] private TextMeshProUGUI  _label;
    [SerializeField] private Image            _background;

    private static readonly Color COLOR_COMPLETED = new Color(0.5f, 0.7f, 0.4f);
    private static readonly Color COLOR_AVAILABLE = new Color(0.85f, 0.7f, 0.3f);
    private static readonly Color COLOR_LOCKED    = new Color(0.3f, 0.3f, 0.3f);

    private BiomeDataSO _biome;
    private int         _levelIndex;
    private BiomeView   _biomeView;
    private bool        _isUnlocked;

    public void Setup(BiomeDataSO biome, int levelIndex, BiomeView biomeView)
    {
        _biome      = biome;
        _levelIndex = levelIndex;
        _biomeView  = biomeView;

        var pdm = PlayerDataManager.Instance;
        string levelId  = biome.GetLevelId(levelIndex);
        bool completed  = pdm != null && pdm.IsLevelCompleted(levelId);
        _isUnlocked     = pdm != null && pdm.IsLevelUnlocked(biome, levelIndex);

        if (_label != null)
        {
            string prefix = completed ? "✓ " : (_isUnlocked ? "" : "🔒 ");
            _label.text   = $"{prefix}{biome.GetLevelDisplayName(levelIndex)}";
        }

        if (_background != null)
        {
            if (completed)        _background.color = COLOR_COMPLETED;
            else if (_isUnlocked) _background.color = COLOR_AVAILABLE;
            else                  _background.color = COLOR_LOCKED;
        }

        if (_button != null)
        {
            _button.interactable = _isUnlocked;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (!_isUnlocked || _biomeView == null) return;
        _biomeView.OnLevelClicked(_biome, _levelIndex);
    }
}
