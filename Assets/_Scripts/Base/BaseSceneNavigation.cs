using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Управление переходами между сценами с BaseScene.
/// Кнопки: "Battle" → MainMenu (выбор биома → бой).
/// </summary>
public class BaseSceneNavigation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _battleButton;

    [Header("Названия сцен")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (_battleButton != null)
            _battleButton.onClick.AddListener(OnBattleClicked);
    }

    private void OnBattleClicked()
    {
        Debug.Log("[BaseScene] Переход в MainMenu для выбора биома");
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
