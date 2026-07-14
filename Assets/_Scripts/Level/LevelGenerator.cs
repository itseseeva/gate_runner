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
        // Если есть LevelLauncher с выбранным уровнем — используем его конфиг
        if (LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedLevel != null)
        {
            _config = LevelLauncher.Instance.SelectedLevel.GenerationConfig;
            int lvlIndex = LevelLauncher.Instance.SelectedLevelIndex;
            Debug.Log($"[LevelGen] Использую конфиг из шаблона, уровень={lvlIndex + 1}", this);
        }

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

        float threshold = _leader.position.z - 20f;
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

        // Номер уровня в биоме определяет сложность
        int currentLevel = LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedLevelIndex >= 0
            ? LevelLauncher.Instance.SelectedLevelIndex + 1
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

            // Формация: рандом среди 3 вариантов
            WaveFormation formation = (WaveFormation)Random.Range(0, 3);

            _plan.Waves.Add(new WaveData
            {
                Z                = currentZ,
                EnemyCount       = Random.Range(_config.EnemiesPerWaveMin, _config.EnemiesPerWaveMax + 1),
                HealthMultiplier = totalMul,
                Formation        = formation,
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
        if (_config.EnemyPrefabs == null || _config.EnemyPrefabs.Count == 0) return;

        // Определяем позиции врагов в зависимости от формации
        Vector3[] positions = GenerateWavePositions(wave);

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject prefabToSpawn = _config.EnemyPrefabs[Random.Range(0, _config.EnemyPrefabs.Count)];

            if (prefabToSpawn == null) continue;

            GameObject go = Instantiate(prefabToSpawn, positions[i], Quaternion.identity);

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyHealthMultiplier(wave.HealthMultiplier);
                _aliveEnemyCount++;
            }
        }

        Debug.Log($"[LevelGen] Волна заспавнена на Z={wave.Z:F0}, формация={wave.Formation}, врагов={wave.EnemyCount}, HP×{wave.HealthMultiplier:F2}", this);
    }

    /// <summary>Генерирует позиции для волны в зависимости от формации.</summary>
    private Vector3[] GenerateWavePositions(WaveData wave)
    {
        Vector3[] positions = new Vector3[wave.EnemyCount];

        switch (wave.Formation)
        {
            case WaveFormation.LeftCluster:
                // Плотный отряд слева (X ≈ -1.2)
                positions = GenerateClusterPositions(wave.EnemyCount, wave.Z, centerX: -1.2f, clusterRadius: 0.8f);
                break;

            case WaveFormation.RightCluster:
                // Плотный отряд справа (X ≈ +1.2)
                positions = GenerateClusterPositions(wave.EnemyCount, wave.Z, centerX: 1.2f, clusterRadius: 0.8f);
                break;

            case WaveFormation.CenterMob:
                // Большая толпа по центру — широкая по X и глубокая по Z
                positions = GenerateClusterPositions(wave.EnemyCount, wave.Z, centerX: 0f, clusterRadius: 1.5f);
                break;
        }

        return positions;
    }

    /// <summary>Генерирует позиции толпы вокруг указанного центра.</summary>
    private Vector3[] GenerateClusterPositions(int count, float centerZ, float centerX, float clusterRadius)
    {
        Vector3[] positions = new Vector3[count];

        // Расставляем в сетке + лёгкий шум для естественности
        int rows = Mathf.CeilToInt(Mathf.Sqrt(count));
        int cols = Mathf.CeilToInt((float)count / rows);
        float spacing = clusterRadius * 2f / Mathf.Max(cols - 1, 1);

        int idx = 0;
        for (int row = 0; row < rows && idx < count; row++)
        {
            for (int col = 0; col < cols && idx < count; col++)
            {
                float x = centerX + (col - (cols - 1) / 2f) * spacing;
                float z = centerZ + (row - (rows - 1) / 2f) * spacing;

                // Лёгкий случайный шум чтобы не выглядело как сетка
                x += Random.Range(-0.15f, 0.15f);
                z += Random.Range(-0.15f, 0.15f);

                positions[idx] = new Vector3(x, 1f, z);
                idx++;
            }
        }

        return positions;
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

    /// <summary>Принудительно завершает уровень победой. Используется чит-панелью.</summary>
    [ContextMenu("Force Victory")]
    public void ForceFinishLevel()
    {
        if (_levelFinished) return;
        _levelFinished = true;
        FinishLevel();
    }

    private void FinishLevel()
    {
        Debug.Log("[LevelGen] УРОВЕНЬ ПРОЙДЕН!", this);

        // НЕ начисляем награды здесь — это делает VictoryUI после анимации
        // Просто помечаем уровень пройденным
        var launcher = LevelLauncher.Instance;
        var pdm      = PlayerDataManager.Instance;

        if (launcher != null && pdm != null && !string.IsNullOrEmpty(launcher.SelectedLevelId))
        {
            pdm.MarkLevelComplete(launcher.SelectedLevelId);
        }

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
