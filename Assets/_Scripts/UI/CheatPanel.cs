using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель с чит-кнопками для разработки.
/// TODO: убрать в релизной версии или скрыть за паролем.
/// </summary>
public class CheatPanel : MonoBehaviour
{
    [SerializeField] private Button _victoryButton;
    [SerializeField] private Button _gameOverButton;

    [Header("Слоу Мо")]
    [SerializeField] private Button _slowMoButton;
    [SerializeField] private TextMeshProUGUI _slowMoLabel;

    // TODO: вынести в RemoteConfig
    [SerializeField] private float _slowMoScale = 0.2f;

    private bool _isSlowMo = false;

    private void Start()
    {
        if (_victoryButton != null)
            _victoryButton.onClick.AddListener(ForceVictory);

        if (_gameOverButton != null)
            _gameOverButton.onClick.AddListener(ForceGameOver);

        if (_slowMoButton != null)
            _slowMoButton.onClick.AddListener(ToggleSlowMo);

        UpdateSlowMoLabel();
    }

    private void ForceVictory()
    {
        Debug.Log("[Cheat] Принудительная победа", this);
        LevelGenerator gen = FindAnyObjectByType<LevelGenerator>();
        if (gen != null)
            gen.ForceFinishLevel();
        else if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetVictory();
    }

    private void ForceGameOver()
    {
        Debug.Log("[Cheat] Принудительный Game Over", this);
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetGameOver();
    }

    /// <summary>Переключает слоу мо вкл/выкл.</summary>
    private void ToggleSlowMo()
    {
        _isSlowMo = !_isSlowMo;
        Time.timeScale = _isSlowMo ? _slowMoScale : 1f;
        Debug.Log($"[Cheat] SlowMo {(_isSlowMo ? "ON" : "OFF")} timeScale={Time.timeScale}", this);
        UpdateSlowMoLabel();
    }

    private void UpdateSlowMoLabel()
    {
        if (_slowMoLabel != null)
            _slowMoLabel.text = _isSlowMo ? "SLOW: ON" : "SLOW: OFF";
    }

    private void OnDestroy()
    {
        // Сбрасываем timeScale при уничтожении панели
        Time.timeScale = 1f;
    }
}
