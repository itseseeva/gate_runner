using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Главный класс процедурной генерации уровня.
/// 1. При старте создаёт LevelPlan по правилам из GenerationConfigSO
/// 2. Каждый кадр lazy-спавнит объекты которые близко к лидеру
/// 3. Следит за финишем и триггерит победу
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private GenerationConfigSO _config;
    [SerializeField] private Transform          _leader;

    [Header("Lazy spawn")]
    [Tooltip("На какой дистанции от лидера спавнить объекты")]
    [SerializeField] private float _spawnAheadDistance = 50f;

    private LevelPlan _plan;
    private bool      _levelFinished;
    private float     _virtualLeaderZ;
    private int       _aliveEnemyCount = 0;  // сколько врагов сейчас живо на сцене

    private void Start()
    {
        if (_config == null)
        {
            Debug.LogError("[LevelGen] GenerationConfig не задан!", this);
            return;
        }

        if (_leader == null)
        {
            // Автопоиск SquadLeader на сцене
            var squad = FindAnyObjectByType<SquadController>();
            if (squad != null) _leader = squad.transform;
        }

        BuildPlan();
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
        Debug.Log($"[LevelGen] План уровня готов: {_plan.Waves.Count} волн, {_plan.Gates.Count} ворот, длина {_plan.LevelLength}м", this);
    }

    private void OnDestroy()
    {
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy e)
    {
        _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
    }

    private void Update()
    {
        if (_plan == null || _leader == null || _levelFinished) return;

        // Замираем при Game Over
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        // Лидер не двигается — мир сам едет на него через WorldScroller.
        // Накапливаем "виртуальное расстояние" которое прошёл лидер.
        _virtualLeaderZ += WorldScroller.WorldSpeed * Time.deltaTime;

        float spawnLineZ = _virtualLeaderZ + _spawnAheadDistance;

        // Спавним волны которые в зоне видимости
        foreach (WaveData wave in _plan.Waves)
        {
            if (wave.Spawned) continue;
            if (wave.Z > spawnLineZ) continue;
            SpawnWave(wave);
            wave.Spawned = true;
        }

        // Спавним ворота
        foreach (GateData gate in _plan.Gates)
        {
            if (gate.Spawned) continue;
            if (gate.Z > spawnLineZ) continue;
            SpawnGate(gate);
            gate.Spawned = true;
        }

        // ─── Очистка врагов которые уехали за лидера ────────────
        CleanupEscapedEnemies();

        // Финиш — все волны заспавнены И все враги убиты
        if (AllWavesSpawned() && GetTotalEnemiesRemaining() == 0)
        {
            _levelFinished = true;
            FinishLevel();
        }
        else if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[LevelGen] Финиш проверка: AllWavesSpawned={AllWavesSpawned()}, врагов={GetTotalEnemiesRemaining()}");
        }
    }

    private void CleanupEscapedEnemies()
    {
        if (_leader == null) return;

        float threshold = _leader.position.z - 0.5f;
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy e in enemies)
        {
            if (e == null || !e.gameObject.activeSelf) continue;
            if (e.transform.position.z < threshold)
            {
                _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
                e.gameObject.SetActive(false);
                Debug.Log($"[LevelGen] Враг убежал за лидера, деактивирован", this);
            }
        }
    }

    // ─── Построение плана ────────────────────────────────────────

    private void BuildPlan()
    {
        _plan = new LevelPlan { LevelLength = _config.LevelLength };

        int currentLevel = ProgressionManager.Instance != null
            ? ProgressionManager.Instance.CurrentLevel
            : 1;

        // Множитель сложности от номера уровня
        float levelMul = 1f + currentLevel * _config.LevelScalingPerLevel;

        // Решаем сколько волн
        int waveCount = Random.Range(_config.WaveCountMin, _config.WaveCountMax + 1);

        // Расставляем волны
        float currentZ = _config.FirstWaveZ;
        for (int w = 0; w < waveCount; w++)
        {
            // HP множитель = от Z × от уровня
            float zMul = 1f + currentZ * _config.ZScalingMultiplier;
            float totalMul = zMul * levelMul;

            _plan.Waves.Add(new WaveData
            {
                Z                = currentZ,
                EnemyCount       = Random.Range(_config.EnemiesPerWaveMin, _config.EnemiesPerWaveMax + 1),
                HealthMultiplier = totalMul,
            });

            // Между волнами — ворота
            if (w < waveCount - 1)
            {
                float nextWaveZ = currentZ + Random.Range(_config.WaveSpacingMin, _config.WaveSpacingMax);
                AddGatesBetween(currentZ, nextWaveZ);
                currentZ = nextWaveZ;
            }
        }
    }

    private void AddGatesBetween(float fromZ, float toZ)
    {
        // Ворота посередине, рандом ± 5м
        float middleZ = (fromZ + toZ) / 2f;
        float gateZ   = middleZ + Random.Range(-5f, 5f);

        bool isDouble = Random.value < _config.DoubleGateChance;

        if (isDouble)
        {
            GateData left  = MakeGateData(gateZ, -1.2f);
            GateData right = MakeGateData(gateZ, +1.2f);

            // Гарантируем что вторые ворота — другой prefab
            int attempts = 0;
            while (right.Prefab == left.Prefab && attempts < 10)
            {
                right = MakeGateData(gateZ, +1.2f);
                attempts++;
            }

            // Если оба универсальных GatePair — гарантируем разные настройки
            if (right.Prefab == left.Prefab && right.NeedsRandomQuantity)
            {
                // Если выпало одинаковое (например 2 универсальных) — меняем тип юнита у второго
                while (right.HeroType == left.HeroType)
                {
                    right.HeroType = _config.QuantityHeroPool[Random.Range(0, _config.QuantityHeroPool.Count)];
                }
            }

            _plan.Gates.Add(left);
            _plan.Gates.Add(right);
        }
        else
        {
            // Одна ворота — рандомно слева или справа
            float side = Random.value < 0.5f ? -1.2f : +1.2f;
            _plan.Gates.Add(MakeGateData(gateZ, side));
        }
    }

    private GateData MakeGateData(float z, float x)
    {
        // Выбираем рандомный prefab из пула
        GameObject prefab = _config.GatePool[Random.Range(0, _config.GatePool.Count)];

        var data = new GateData { Z = z, X = x, Prefab = prefab };

        // Если это универсальный GatePair (нет ElementGate в нём) — настраиваем Quantity рандомно
        // Проверяем по компоненту: если на prefab висит ElementGate — это уже настроенный Element prefab
        bool isElement = prefab.GetComponentInChildren<ElementGate>(true) != null;

        if (!isElement)
        {
            data.NeedsRandomQuantity = true;
            data.HeroType   = _config.QuantityHeroPool[Random.Range(0, _config.QuantityHeroPool.Count)];
            data.IsMultiply = Random.value < _config.MultiplyChance;

            if (data.IsMultiply)
            {
                data.Value = Random.Range(_config.MultiplyValueRange.x, _config.MultiplyValueRange.y + 1);
            }
            else if (Random.value < _config.NegativeChance)
            {
                int abs = Random.Range(_config.NegativeValueRange.x, _config.NegativeValueRange.y + 1);
                data.Value = -abs;
            }
            else
            {
                data.Value = Random.Range(_config.AddValueRange.x, _config.AddValueRange.y + 1);
            }
        }

        return data;
    }

    // ─── Спавн ───────────────────────────────────────────────────

    private void SpawnWave(WaveData wave)
    {
        if (_config.EnemyPrefab == null) return;

        for (int i = 0; i < wave.EnemyCount; i++)
        {
            float totalWidth = (wave.EnemyCount - 1) * _config.EnemySpreadX;
            float x = -totalWidth / 2f + i * _config.EnemySpreadX;

            GameObject go = Instantiate(_config.EnemyPrefab, new Vector3(x, 1f, wave.Z), Quaternion.identity);

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyHealthMultiplier(wave.HealthMultiplier);
                _aliveEnemyCount++;
            }
        }

        Debug.Log($"[LevelGen] Волна заспавнена на Z={wave.Z:F0}, врагов={wave.EnemyCount}, HP×{wave.HealthMultiplier:F2}", this);
    }

    private void SpawnGate(GateData data)
    {
        GameObject go = Instantiate(data.Prefab, new Vector3(data.X, 0.01f, data.Z), Quaternion.identity);
        Debug.Log($"[LevelGen] Спавню ворота {data.Prefab.name} на X={data.X:F1} Z={data.Z:F0}", this);

        // Если универсальный — настраиваем QuantityGate на лету
        if (data.NeedsRandomQuantity)
        {
            QuantityGate gate = go.GetComponentInChildren<QuantityGate>(true);
            if (gate != null)
            {
                gate.SetupForGenerator(data.HeroType, data.IsMultiply, data.Value);
            }
        }
    }

    // ─── Финиш ───────────────────────────────────────────────────

    private void FinishLevel()
    {
        Debug.Log("[LevelGen] УРОВЕНЬ ПРОЙДЕН!", this);

        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.AdvanceLevel();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetVictory();
    }

    /// <summary>
    /// Возвращает сколько врагов осталось убить (живые на сцене + ещё не заспавненные).
    /// </summary>
    public int GetTotalEnemiesRemaining()
    {
        int notSpawned = 0;
        if (_plan != null)
        {
            foreach (WaveData wave in _plan.Waves)
            {
                if (!wave.Spawned) notSpawned += wave.EnemyCount;
            }
        }

        return _aliveEnemyCount + notSpawned;
    }

    /// <summary>True если все запланированные волны уже заспавнены.</summary>
    private bool AllWavesSpawned()
    {
        if (_plan == null) return false;
        foreach (WaveData wave in _plan.Waves)
            if (!wave.Spawned) return false;
        return true;
    }
}
