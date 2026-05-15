using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Экран Победы. Появляется когда игрок прошёл уровень.
/// Кнопка "Дальше" перезагружает сцену → следующий уровень.
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _levelLabel;

    private void Start()
    {
        if (_panel != null) _panel.SetActive(false);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(OnContinue);

        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState != GameState.Victory) return;

        if (_panel != null) _panel.SetActive(true);

        if (_levelLabel != null && ProgressionManager.Instance != null)
        {
            // Показываем какой только что прошли (после AdvanceLevel это уже следующий)
            int prev = ProgressionManager.Instance.CurrentLevel - 1;
            _levelLabel.text = $"Уровень {prev} пройден!";
        }
    }

    private void OnContinue()
    {
        // Возврат в главное меню
        SceneManager.LoadScene("MainMenu");
    }
}
