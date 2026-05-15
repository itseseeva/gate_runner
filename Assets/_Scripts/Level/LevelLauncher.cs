using UnityEngine;

/// <summary>
/// DontDestroyOnLoad-объект который хранит "какой уровень сейчас выбран".
/// Используется для передачи данных между сценами MainMenu → Gameplay.
///
/// Хранит биом и индекс уровня. LevelGenerator берёт отсюда конфиг для генерации.
/// </summary>
public class LevelLauncher : MonoBehaviour
{
    public static LevelLauncher Instance { get; private set; }

    public BiomeDataSO SelectedBiome { get; private set; }
    public int         SelectedLevelIndex { get; private set; } = -1;

    /// <summary>Шаблон выбранного уровня для геймплея.</summary>
    public LevelDataSO SelectedLevel => SelectedBiome != null ? SelectedBiome.LevelTemplate : null;

    /// <summary>Уникальный ID выбранного уровня (например "forest_3").</summary>
    public string SelectedLevelId => SelectedBiome != null
        ? SelectedBiome.GetLevelId(SelectedLevelIndex)
        : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Выбирает уровень для запуска.</summary>
    public void SelectLevel(BiomeDataSO biome, int levelIndex)
    {
        SelectedBiome      = biome;
        SelectedLevelIndex = levelIndex;
        Debug.Log($"[LevelLauncher] Выбран: {biome.DisplayName} → {biome.GetLevelDisplayName(levelIndex)}", this);
    }
}
