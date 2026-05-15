using UnityEngine;

/// <summary>
/// Данные одного уровня в биоме.
/// Хранит ссылку на правила генерации и награды за прохождение.
///
/// Id должен быть уникальным (например "forest_1", "forest_7").
/// По нему PlayerDataManager отслеживает прогресс.
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "MGR/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Идентификация")]
    [Tooltip("Уникальный ID уровня (например 'forest_1'). По нему сохраняется прогресс.")]
    public string Id = "biome_X";

    [Tooltip("Отображаемое имя в UI")]
    public string DisplayName = "Уровень 1";

    [Header("Геймплей")]
    [Tooltip("Какой конфиг генерации использовать")]
    public GenerationConfigSO GenerationConfig;

    [Header("Награды за победу")]
    [Min(0)] public int RewardGold = 10;
    [Min(0)] public int RewardXP   = 5;
}
