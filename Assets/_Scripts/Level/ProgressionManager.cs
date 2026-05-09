using UnityEngine;

/// <summary>
/// Хранит прогресс игрока — текущий номер уровня.
/// Сохраняется в PlayerPrefs (локально на устройстве).
/// При старте сцены доступен через ProgressionManager.Instance.
///
/// TODO: при подключении UGS заменить PlayerPrefs на Cloud Save.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    private const string KEY_CURRENT_LEVEL = "mgr_current_level";

    private int _currentLevel;

    /// <summary>
    /// Номер текущего уровня. 1 — самый первый. Растёт после каждой победы.
    /// </summary>
    public int CurrentLevel => _currentLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Load()
    {
        // Если ключа нет — значит первый запуск. Стартуем с 1.
        _currentLevel = PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        Debug.Log($"[Progression] Загружен уровень: {_currentLevel}", this);
    }

    /// <summary>Победил уровень — переходим на следующий.</summary>
    public void AdvanceLevel()
    {
        _currentLevel++;
        Save();
        Debug.Log($"[Progression] Перешли на уровень {_currentLevel}", this);
    }

    /// <summary>Сбросить прогресс (для теста или из настроек).</summary>
    [ContextMenu("Сбросить прогресс")]
    public void ResetProgress()
    {
        _currentLevel = 1;
        Save();
        Debug.Log("[Progression] Прогресс сброшен", this);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, _currentLevel);
        PlayerPrefs.Save();
    }
}
