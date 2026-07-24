using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экран Game Over. Показывается при смене состояния на GameOver.
/// Кнопка Restart перезагружает текущую сцену.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject _panel;          // GameOverPanel
    [SerializeField] private Button     _restartButton;  // кнопка перезапуска

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleStateChanged;

        if (_restartButton != null)
            _restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;

        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(RestartGame);
    }

    private void Start()
    {
        // По умолчанию панель скрыта
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void HandleStateChanged(GameState newState)
    {
        if (_panel == null) return;

        // Показываем панель только при GameOver
        _panel.SetActive(newState == GameState.GameOver);
    }

    /// <summary>Перезапускает текущую сцену.</summary>
    public void RestartGame()
    {
        {}

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
