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

    [Header("Скорость отряда")]
    [SerializeField] private Button _slowSquadButton;
    [SerializeField] private TextMeshProUGUI _slowSquadLabel;

    // TODO: вынести в RemoteConfig
    [SerializeField] private float _slowMoScale = 0.2f;

    private bool _isSlowMo = false;
    private bool _isSlowSquad = false;

    private void Start()
    {
        if (_victoryButton != null)
            _victoryButton.onClick.AddListener(ForceVictory);

        if (_gameOverButton != null)
            _gameOverButton.onClick.AddListener(ForceGameOver);

        if (_slowMoButton != null)
            _slowMoButton.onClick.AddListener(ToggleSlowMo);

        if (_slowSquadButton != null)
            _slowSquadButton.onClick.AddListener(ToggleSlowSquad);

        UpdateSlowMoLabel();
        UpdateSlowSquadLabel();
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

    private void ToggleSlowSquad()
    {
        _isSlowSquad = !_isSlowSquad;
        WorldScroller.WorldSpeed = _isSlowSquad ? 2f : 7f;
        Debug.Log($"[Cheat] WorldSpeed={WorldScroller.WorldSpeed}", this);
        UpdateSlowSquadLabel();
    }

    private void UpdateSlowSquadLabel()
    {
        if (_slowSquadLabel != null)
            _slowSquadLabel.text = _isSlowSquad ? "SPEED: SLOW" : "SPEED: NORM";
    }

    private void OnDestroy()
    {
        // Сбрасываем timeScale при уничтожении панели
        Time.timeScale = 1f;
        WorldScroller.WorldSpeed = 7f;
    }
}
