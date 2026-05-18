using UnityEngine;

/// <summary>
/// Данные одного биома (Forest, Desert, Cave...).
/// Уровни генерируются автоматически из шаблона на основе LevelCount.
/// </summary>
[CreateAssetMenu(fileName = "Biome", menuName = "MGR/Biome Data")]
public class BiomeDataSO : ScriptableObject
{
    [Header("Идентификация")]
    [Tooltip("Уникальный ID биома: 'forest', 'desert' и т.д.")]
    public string Id = "biome_id";

    [Tooltip("Отображаемое имя биома")]
    public string DisplayName = "Forest";

    [Tooltip("Иконка биома для меню (опционально)")]
    public Sprite Icon;

    [Header("Уровни биома (авто-генерация)")]
    [Tooltip("Сколько уровней в биоме")]
    [Min(1)] public int LevelCount = 10;

    [Tooltip("Шаблон уровня. Содержит GenerationConfig + базовые награды. На основе шаблона создаются все N уровней.")]
    public LevelDataSO LevelTemplate;

    /// <summary>
    /// Возвращает уникальный ID для конкретного уровня биома.
    /// Например для biome 'forest', level 3 → 'forest_3'.
    /// </summary>
    public string GetLevelId(int levelIndex)
    {
        // levelIndex от 0 до LevelCount-1
        return $"{Id}_{levelIndex + 1}";
    }

    /// <summary>
    /// Отображаемое имя уровня. Например "Уровень 3".
    /// </summary>
    public string GetLevelDisplayName(int levelIndex)
    {
        return $"Уровень {levelIndex + 1}";
    }

    /// <summary>
    /// Награда золотом за прохождение конкретного уровня.
    /// Растёт линейно от LevelTemplate.RewardGold × (1..LevelCount).
    /// </summary>
    public int GetLevelRewardGold(int levelIndex)
    {
        if (LevelTemplate == null) return 10;
        return LevelTemplate.RewardGold * (levelIndex + 1);
    }

    /// <summary>Награда опытом — аналогично золоту.</summary>
    public int GetLevelRewardXP(int levelIndex)
    {
        if (LevelTemplate == null) return 5;
        return LevelTemplate.RewardXP * (levelIndex + 1);
    }

    /// <summary>Награда iron — растёт линейно от LevelTemplate.RewardIron × (1..LevelCount).</summary>
    public int GetLevelRewardIron(int levelIndex)
    {
        if (LevelTemplate == null) return 3;
        return LevelTemplate.RewardIron * (levelIndex + 1);
    }
}
