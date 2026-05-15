using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Экран Победы. Появляется когда игрок прошёл уровень.
/// Показывает что прошёл и какие награды получил.
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject       _panel;
    [SerializeField] private Button           _continueButton;
    [SerializeField] private TextMeshProUGUI  _levelLabel;
    [SerializeField] private TextMeshProUGUI  _rewardsLabel;

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

        // Имя уровня
        if (_levelLabel != null && LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedBiome != null)
        {
            var biome = LevelLauncher.Instance.SelectedBiome;
            int idx   = LevelLauncher.Instance.SelectedLevelIndex;
            _levelLabel.text = $"{biome.DisplayName} — {biome.GetLevelDisplayName(idx)} пройден!";
        }

        // Текст наград
        if (_rewardsLabel != null && LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedBiome != null)
        {
            var biome = LevelLauncher.Instance.SelectedBiome;
            int idx   = LevelLauncher.Instance.SelectedLevelIndex;
            int gold  = biome.GetLevelRewardGold(idx);
            int xp    = biome.GetLevelRewardXP(idx);
            _rewardsLabel.text = $"+{gold} Gold\n+{xp} XP";
        }
    }

    private void OnContinue()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
