using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель с чит-кнопками для разработки.
/// TODO: убрать в релизной версии или скрыть за паролем.
/// </summary>
public class CheatPanel : MonoBehaviour
{
    [SerializeField] private Button _victoryButton;
    [SerializeField] private Button _gameOverButton;

    private void Start()
    {
        if (_victoryButton != null)
            _victoryButton.onClick.AddListener(ForceVictory);

        if (_gameOverButton != null)
            _gameOverButton.onClick.AddListener(ForceGameOver);
    }

    private void ForceVictory()
    {
        Debug.Log("[Cheat] Принудительная победа", this);

        // Используем LevelGenerator для нормального завершения уровня
        // (он сам начислит награды через PlayerDataManager)
        LevelGenerator gen = FindAnyObjectByType<LevelGenerator>();
        if (gen != null)
        {
            gen.ForceFinishLevel();
        }
        else if (GameStateManager.Instance != null)
        {
            // Fallback на случай если генератор не найден
            GameStateManager.Instance.SetVictory();
        }
    }

    private void ForceGameOver()
    {
        Debug.Log("[Cheat] Принудительный Game Over", this);
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetGameOver();
    }
}
