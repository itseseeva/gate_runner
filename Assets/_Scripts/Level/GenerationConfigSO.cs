using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Правила процедурной генерации уровней.
/// Один SO на всю игру (пока). При старте уровня LevelGenerator читает эти правила
/// и создаёт уникальный набор волн и ворот.
///
/// Сложность растёт двумя способами:
/// 1) ВНУТРИ уровня — HP врагов × ZScalingMultiplier на каждый метр Z
/// 2) МЕЖДУ уровнями — HP × (1 + LevelNumber × LevelScalingPerLevel)
/// </summary>
[CreateAssetMenu(fileName = "GenerationConfig", menuName = "MGR/Generation Config")]
public class GenerationConfigSO : ScriptableObject
{
    // ─── Длина уровня ────────────────────────────────────────────
    [Header("Длина уровня")]
    [Tooltip("Длина уровня в метрах. При скорости ~9.2 м/с — 35 секунд = ~320м.")]
    public float LevelLength = 300f;

    // ─── Волны ───────────────────────────────────────────────────
    [Header("Волны врагов")]
    [Min(1)] public int WaveCountMin       = 4;
    [Min(1)] public int WaveCountMax       = 7;

    [Min(1)] public int EnemiesPerWaveMin  = 8;
    [Min(1)] public int EnemiesPerWaveMax  = 15;

    [Tooltip("Мин расстояние между волнами по Z, метры")]
    public float WaveSpacingMin = 30f;
    [Tooltip("Макс расстояние между волнами по Z, метры")]
    public float WaveSpacingMax = 50f;

    [Tooltip("С какого Z спавнится первая волна")]
    public float FirstWaveZ = 30f;

    [Tooltip("Разброс врагов по X внутри волны")]
    public float EnemySpreadX = 1.5f;

    [Tooltip("Список префабов врагов. Если здесь несколько, генератор будет выбирать их случайно.")]
    public List<GameObject> EnemyPrefabs = new();

    // ─── Ворота ──────────────────────────────────────────────────
    [Header("Ворота — простой пул префабов")]
    [Tooltip("Все префабы ворот которые могут появиться в уровне. Element-ворота добавляются 'как есть', Quantity-ворота генератор настраивает случайно.")]
    public List<GameObject> GatePool = new();

    [Tooltip("Шанс что между волнами появятся ДВОЕ ворот (выбор слева/справа). 0.2 = 20%. Иначе одна.")]
    [Range(0f, 1f)] public float DoubleGateChance = 0.2f;

    [Header("Случайные настройки Quantity-ворот")]
    [Tooltip("Список возможных Hero Type для Quantity-ворот.")]
    public List<HeroType> QuantityHeroPool = new()
    {
        HeroType.Mage,
        HeroType.Archer,
        HeroType.Warrior,
        HeroType.Tank,
    };

    [Tooltip("Шанс что Quantity-ворота будут на УМНОЖЕНИЕ (×N) вместо +N. 0 = всегда Add, 1 = всегда Multiply.")]
    [Range(0f, 1f)] public float MultiplyChance = 0.2f;

    [Tooltip("Шанс что Quantity-ворота будут с ОТРИЦАТЕЛЬНЫМ значением (-N юнитов). 0 = всегда позитивные.")]
    [Range(0f, 1f)] public float NegativeChance = 0.15f;

    [Tooltip("Диапазон значений для Add-ворот: +N юнитов")]
    public Vector2Int AddValueRange = new Vector2Int(2, 6);

    [Tooltip("Диапазон значений для Multiply-ворот: ×N")]
    public Vector2Int MultiplyValueRange = new Vector2Int(2, 3);

    [Tooltip("Диапазон значений для Negative-ворот: -N юнитов")]
    public Vector2Int NegativeValueRange = new Vector2Int(2, 4);

    // ─── Сложность ───────────────────────────────────────────────
    [Header("Сложность")]
    [Tooltip("HP врага = BaseHP × (1 + Z × этот коэффициент). 0.005 = +100% на Z=200")]
    public float ZScalingMultiplier = 0.005f;

    [Tooltip("HP врага дополнительно × (1 + LevelNumber × этот коэффициент). 0.2 = +20% на каждый уровень")]
    public float LevelScalingPerLevel = 0.2f;
}


