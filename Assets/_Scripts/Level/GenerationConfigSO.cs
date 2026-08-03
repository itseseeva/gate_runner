using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

/// <summary>
/// Правила генерации уровня: ритм волн (кривая интенсивности) и разнообразие типов врагов.
/// Один SO на конфигурацию. LevelGenerator читает и строит план уровня.
/// </summary>
[CreateAssetMenu(fileName = "GenerationConfig", menuName = "MGR/Generation Config")]
public class GenerationConfigSO : ScriptableObject
{

    [Header("━━━ Сколько волн ━━━")]
    [Tooltip("Минимум волн за уровень.")]
    [FormerlySerializedAs("WaveCountMin")]
    [Min(1)] public int MinWaves = 6;

    [Tooltip("Максимум волн за уровень.")]
    [FormerlySerializedAs("WaveCountMax")]
    [Min(1)] public int MaxWaves = 10;

    [Header("━━━ Ритм напряжения ━━━")]
    [Tooltip("Сколько ПИКОВ (наплывов) за уровень. 3 = три волны-'горки' с передышками.")]
    [FormerlySerializedAs("IntensityPeaks")]
    [Min(1)] public int PressurePeaks = 3;

    [Tooltip("Врагов в одной полосе в ТИХОЙ волне (передышка).")]
    [FormerlySerializedAs("EnemiesPerLaneMin")]
    [Min(0)] public int EnemiesPerLane_Calm = 2;

    [Tooltip("Врагов в одной полосе на ПИКЕ (наплыв).")]
    [FormerlySerializedAs("EnemiesPerLaneMax")]
    [Min(1)] public int EnemiesPerLane_Peak = 6;

    [Header("━━━ Расстояние между волнами ━━━")]
    [Tooltip("Минимальный промежуток между волнами по Z, метры.")]
    [FormerlySerializedAs("WaveSpacingMin")]
    public float GapBetweenWavesMin = 18f;

    [Tooltip("Максимальный промежуток между волнами по Z, метры.")]
    [FormerlySerializedAs("WaveSpacingMax")]
    public float GapBetweenWavesMax = 28f;

    [Tooltip("На каком Z встретится ПЕРВАЯ волна (как далеко от старта).")]
    [FormerlySerializedAs("FirstWaveZ")]
    public float FirstWaveDistance = 25f;

    [Header("━━━ Враги ━━━")]
    [Tooltip("Все префабы врагов, из которых собираются волны.")]
    [FormerlySerializedAs("EnemyPrefabs")]
    public List<GameObject> EnemyPrefabs = new();

    [Tooltip("Доля опасных типов (маги/роллеры) на ПИКЕ. 0.5 = половина волны опасные, тихие волны — почти чистый melee.")]
    [FormerlySerializedAs("DangerRatioAtPeak")]
    [Range(0f, 1f)] public float DangerRatioAtPeak = 0.5f;

    [Header("━━━ Ворота ━━━")]
    [Tooltip("Префабы ворот. Element — как есть, Quantity — настраиваются генератором.")]
    public List<GameObject> GatePool = new();

    [Tooltip("Шанс двойных ворот (выбор слева/справа). 0.2 = 20%.")]
    [Range(0f, 1f)] public float DoubleGateChance = 0.2f;

    [Header("━━━ Настройки Quantity-ворот ━━━")]
    [Tooltip("Из каких типов героев генерятся Quantity-ворота.")]
    public List<HeroType> QuantityHeroPool = new()
    {
        HeroType.Mage, HeroType.Archer, HeroType.Warrior, HeroType.Tank,
    };

    [Tooltip("Шанс ворот на умножение (×N) вместо (+N).")]
    [Range(0f, 1f)] public float MultiplyChance = 0.2f;

    [Tooltip("Шанс отрицательных ворот (-N юнитов).")]
    [Range(0f, 1f)] public float NegativeChance = 0.15f;

    [Tooltip("Диапазон для (+N) ворот.")]
    public Vector2Int AddValueRange = new Vector2Int(2, 6);

    [Tooltip("Диапазон для (×N) ворот.")]
    public Vector2Int MultiplyValueRange = new Vector2Int(2, 3);

    [Tooltip("Диапазон для (-N) ворот.")]
    public Vector2Int NegativeValueRange = new Vector2Int(2, 4);

    [Header("━━━ Сложность ━━━")]
    [Tooltip("HP × (1 + Z × коэфф). Рост по ходу уровня.")]
    [FormerlySerializedAs("ZScalingMultiplier")]
    public float HpScalingPerMeter = 0.005f;

    [Tooltip("HP × (1 + номер_уровня × коэфф). Рост между уровнями.")]
    [FormerlySerializedAs("LevelScalingPerLevel")]
    public float HpScalingPerLevel = 0.2f;

    [Header("━━━ ТЕСТОВЫЙ РЕЖИМ ━━━")]
    [Tooltip("Включить тест-режим (заменяет генерацию ворот)")]
    public bool TestMode_ForceGates = false;
    
    [Tooltip("Стихия ворот для тест-режима")]
    public ElementType TestMode_Element = ElementType.Fire;
    
    [Tooltip("Сколько ворот спавнить подряд")]
    public int TestMode_GatesPerSpot = 3;
}
