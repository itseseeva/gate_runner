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
    [Tooltip("На какой дистанции от лидера спавнить объекты (строго 37 метров)")]
    [SerializeField] private float _spawnAheadDistance = 37f;

    private LevelPlan _plan;
    private bool      _levelFinished;
    private float     _virtualLeaderZ;
    private int       _aliveEnemyCount  = 0;  // сколько врагов сейчас живо на сцене
    private int       _waveSpawnCounter = 0;  // для равномерной раздачи полос по кругу

    private void Awake()
    {
        _spawnAheadDistance = 37f;
    }

    private void Start()
    {
        // Если есть LevelLauncher с выбранным уровнем — используем его конфиг
        if (LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedLevel != null)
        {
            _config = LevelLauncher.Instance.SelectedLevel.GenerationConfig;
            int lvlIndex = LevelLauncher.Instance.SelectedLevelIndex;
            {}
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
        {}
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

        // Спавним сундуки через пул
        foreach (GateData chest in _plan.Chests)
        {
            if (chest.Spawned) continue;
            if (chest.Z > spawnLineZ) continue;
            SpawnChest(chest);
            chest.Spawned = true;
        }

        // ─── Очистка врагов которые уехали за лидера (раз в 30 кадров) ────────────
        if (Time.frameCount % 30 == 0) CleanupEscapedEnemies();

        // ДИАГ (убрать после)
        if (Time.frameCount % 60 == 0)
            Debug.Log($"[Finish] AllWavesSpawned={AllWavesSpawned()} " +
                      $"remaining={GetTotalEnemiesRemaining()} " +
                      $"activeRegistry={EnemyCombatBase.AllEnemies.Count} finished={_levelFinished}");

        // Финиш — все волны заспавнены И все враги убиты
        if (AllWavesSpawned() && GetTotalEnemiesRemaining() == 0)
        {
            _levelFinished = true;
            FinishLevel();
        }
    }

    private void CleanupEscapedEnemies()
    {
        if (_leader == null) return;

        float threshold = _leader.position.z - 20f;

        // Идём по статическому реестру, а не FindObjectsByType —
        // поиск по всей сцене каждый кадр съедал FPS по мере накопления объектов.
        var list = EnemyCombatBase.AllEnemies;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            EnemyCombatBase combat = list[i];
            if (combat == null || !combat.gameObject.activeSelf) continue;
            if (combat.transform.position.z >= threshold) continue;

            Enemy e = combat.GetComponent<Enemy>();
            if (e != null) e.ReturnToPool();
            else combat.gameObject.SetActive(false);
        }
    }

    // ─── Построение плана ────────────────────────────────────────

    private void BuildPlan()
    {
        _plan = new LevelPlan();

        // Номер уровня в биоме определяет сложность
        int currentLevel = LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedLevelIndex >= 0
            ? LevelLauncher.Instance.SelectedLevelIndex + 1
            : 1;

        // Множитель сложности от номера уровня
        float levelMul = 1f + currentLevel * _config.HpScalingPerLevel;

        // Решаем сколько волн
        int waveCount = Random.Range(_config.MinWaves, _config.MaxWaves + 1);

        // Расставляем волны
        float currentZ = _config.FirstWaveDistance;
        for (int w = 0; w < waveCount; w++)
        {
            // Прогресс волны по уровню: 0 (первая) .. 1 (последняя).
            float t = waveCount > 1 ? (float)w / (waveCount - 1) : 1f;

            // Базовое нарастание: линейно ползёт от 0.3 (первая) до 1.0 (последняя).
            // Каждая волна в среднем сильнее предыдущей — это прогрессия.
            float baseline = Mathf.Lerp(0.3f, 1f, t);

            // Лёгкий ритм поверх: небольшие спады-передышки, но не обнуляют прогрессию.
            float ripple = Mathf.Sin(t * Mathf.PI * _config.PressurePeaks * 2f) * 0.15f;

            float intensity = Mathf.Clamp01(baseline + ripple);

            int perLane = Mathf.RoundToInt(Mathf.Lerp(
                _config.EnemiesPerLane_Calm, _config.EnemiesPerLane_Peak, intensity));

            // ДИАГ (убрать после)
            Debug.Log($"[Wave] #{w} t={t:F2} intensity={intensity:F2} perLane={perLane} " +
                      $"calm={_config.EnemiesPerLane_Calm} peak={_config.EnemiesPerLane_Peak}");

            float zMul = 1f + currentZ * _config.HpScalingPerMeter;
            float totalMul = zMul * levelMul;

            _plan.Waves.Add(new WaveData
            {
                Z                = currentZ,
                EnemiesPerLane   = perLane,
                Intensity        = intensity,
                HealthMultiplier = totalMul,
            });

            if (w < waveCount - 1)
            {
                float nextWaveZ = currentZ + Random.Range(_config.GapBetweenWavesMin, _config.GapBetweenWavesMax);
                AddGatesBetween(currentZ, nextWaveZ);

                if (_config.TestMode_SpawnChests && _config.ChestPrefab != null)
                {
                    float chestZ = (currentZ + nextWaveZ) / 2f;
                    _plan.Chests.Add(new GateData { Z = chestZ, X = 0f, Prefab = _config.ChestPrefab });
                }

                currentZ = nextWaveZ;
            }
        }
    }

    private void AddGatesBetween(float fromZ, float toZ)
    {
        // Если GatePool не заполнен в конфиге — пропускаем спавн ворот
        if (_config == null || _config.GatePool == null || _config.GatePool.Count == 0) return;

        // ── ТЕСТ: все три стихии в ряд для сравнения ──
        if (_config.TestMode_ShowAllThree)
        {
            float midZ = (fromZ + toZ) / 2f;

            AddElementGateAt(_config.TestMode_LeftX,   midZ, ElementType.Ice);
            AddElementGateAt(_config.TestMode_CenterX, midZ, ElementType.Lightning);
            AddElementGateAt(_config.TestMode_RightX,  midZ, ElementType.Fire);
            return;
        }

        // ── ТЕСТ-режим: густо ставим ворота одной стихии ──
        if (_config.TestMode_ForceGates)
        {
            float midZ = (fromZ + toZ) / 2f;
            for (int i = 0; i < _config.TestMode_GatesPerSpot; i++)
            {
                float zPos = midZ + i * 3f;                 // 3 метра между воротами в ряду
                GateData g = MakeGateData(zPos, 0f);        // по центру дороги
                if (g != null) _plan.Gates.Add(g);
            }
            return;   // в тест-режиме обычную генерацию ворот пропускаем
        }

        // Ворота посередине, рандом ± 5м
        float middleZ = (fromZ + toZ) / 2f;
        float gateZ   = middleZ + Random.Range(-5f, 5f);

        bool isDouble = Random.value < _config.DoubleGateChance;

        if (isDouble)
        {
            GateData left  = MakeGateData(gateZ, -1.25f);
            GateData right = MakeGateData(gateZ, +1.25f);

            if (left == null || right == null) return;

            // Гарантируем что вторые ворота — другой prefab
            int attempts = 0;
            while (right != null && left != null && right.Prefab == left.Prefab && attempts < 10)
            {
                right = MakeGateData(gateZ, +1.25f);
                attempts++;
            }

            if (left == null || right == null) return;

            // Если оба универсальных GatePair — гарантируем разные настройки (если в пуле больше 1 типа)
            if (right.Prefab == left.Prefab && right.NeedsRandomQuantity)
            {
                var pool = _config.QuantityHeroPool;
                if (pool != null && pool.Count > 1)
                {
                    int attemptsType = 0;
                    while (right.HeroType == left.HeroType && attemptsType < 10)
                    {
                        right.HeroType = pool[Random.Range(0, pool.Count)];
                        attemptsType++;
                    }
                }
            }

            _plan.Gates.Add(left);
            _plan.Gates.Add(right);
        }
        else
        {
            // Одна ворота — рандомно слева или справа
            float side = Random.value < 0.5f ? -1.0f : +1.0f;
            GateData singleGate = MakeGateData(gateZ, side);
            if (singleGate != null)
                _plan.Gates.Add(singleGate);
        }
    }

    /// <summary>
    /// ТЕСТ: ставит ворота указанной стихии в точку (x, z), если такие есть в пуле.
    /// </summary>
    private void AddElementGateAt(float x, float z, ElementType element)
    {
        GameObject prefab = FindElementGateInPool(element);
        if (prefab == null)
        {
            Debug.LogWarning($"[LevelGen][ТЕСТ] Нет ворот стихии {element} в GatePool.", this);
            return;
        }
        _plan.Gates.Add(new GateData { Z = z, X = x, Prefab = prefab });
    }

    /// <summary>
    /// ТЕСТ: находит в GatePool префаб ворот с нужной стихией.
    /// Возвращает null, если таких нет.
    /// </summary>
    private GameObject FindElementGateInPool(ElementType element)
    {
        if (_config.GatePool == null) return null;

        for (int i = 0; i < _config.GatePool.Count; i++)
        {
            GameObject prefab = _config.GatePool[i];
            if (prefab == null) continue;

            ElementGate eg = prefab.GetComponentInChildren<ElementGate>(true);
            if (eg != null && eg.Element == element)
                return prefab;
        }
        return null;
    }

    private GateData MakeGateData(float z, float x)
    {
        if (_config == null || _config.GatePool == null || _config.GatePool.Count == 0) return null;

        // ── ТЕСТ-режим: форсим только выбранную стихию ──
        if (_config.TestMode_ForceGates)
        {
            GameObject forced = FindElementGateInPool(_config.TestMode_Element);
            if (forced != null)
                return new GateData { Z = z, X = x, Prefab = forced };
            Debug.LogWarning($"[LevelGen][ТЕСТ] В GatePool нет ворот стихии {_config.TestMode_Element} — проверь пул.", this);
        }

        // Выбираем рандомный prefab из пула
        GameObject prefab = _config.GatePool[Random.Range(0, _config.GatePool.Count)];
        if (prefab == null) return null;

        var data = new GateData { Z = z, X = x, Prefab = prefab };

        // Если это универсальный GatePair (нет ElementGate в нём) — настраиваем Quantity рандомно
        // Проверяем по компоненту: если на prefab висит ElementGate — это уже настроенный Element prefab
        bool isElement = prefab.GetComponentInChildren<ElementGate>(true) != null;

        if (!isElement)
        {
            data.NeedsRandomQuantity = true;
            if (_config.QuantityHeroPool != null && _config.QuantityHeroPool.Count > 0)
            {
                data.HeroType = _config.QuantityHeroPool[Random.Range(0, _config.QuantityHeroPool.Count)];
            }
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

    /// <summary>Тип врага для смеси волн.</summary>
    private enum EnemyKind { Melee, Mage, Roller }

    private EnemyKind ClassifyEnemy(GameObject prefab)
    {
        if (prefab == null) return EnemyKind.Melee;

        // Роллер — по компоненту EnemyRollCombat.
        if (prefab.GetComponent<EnemyRollCombat>() != null) return EnemyKind.Roller;

        // Маг — по IsRanged в конфиге.
        Enemy e = prefab.GetComponent<Enemy>();
        if (e != null && e.Data != null && e.Data.IsRanged) return EnemyKind.Mage;

        return EnemyKind.Melee;
    }

    /// <summary>
    /// Выбирает врага для полосы с учётом интенсивности волны.
    /// Опасные типы (маг/роллер) появляются чаще на пике и только по бокам.
    /// Центр — всегда melee (таранная толпа).
    /// </summary>
    private GameObject PickEnemy(bool isCenterLane, float intensity)
    {
        var pool = _config.EnemyPrefabs;
        if (pool == null || pool.Count == 0) return null;

        // Шанс, что этот враг будет "опасным" (маг/роллер) — растёт с интенсивностью.
        float dangerChance = intensity * _config.DangerRatioAtPeak;
        bool wantDanger = Random.value < dangerChance;

        // Центр — только melee, никаких магов/роллеров, каким бы ни был пик.
        if (isCenterLane) wantDanger = false;

        // Несколько попыток найти врага нужной категории.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            GameObject candidate = pool[Random.Range(0, pool.Count)];
            EnemyKind kind = ClassifyEnemy(candidate);

            bool isDanger = kind != EnemyKind.Melee;

            if (wantDanger && isDanger) return candidate;
            if (!wantDanger && !isDanger) return candidate;
        }

        // Фолбэк: не нашли нужную категорию — берём хоть melee.
        for (int i = 0; i < pool.Count; i++)
            if (ClassifyEnemy(pool[i]) == EnemyKind.Melee) return pool[i];

        return pool[0];
    }

    private void SpawnWave(WaveData wave)
    {
        if (_config.EnemyPrefabs == null || _config.EnemyPrefabs.Count == 0) return;

        float spawnZ = _leader.position.z + _spawnAheadDistance;

        const float SpawnSpreadX = 0.25f;
        const float SpawnSpreadZ = 0.8f;

        for (int lane = 0; lane < LaneSystem.LaneCount; lane++)
        {
            bool isCenterLane = lane == LaneSystem.LaneCount / 2;
            float laneCenterX = LaneSystem.GetLaneCenterX(lane);

            for (int i = 0; i < wave.EnemiesPerLane; i++)
            {
                float x = laneCenterX + Random.Range(-SpawnSpreadX, SpawnSpreadX);
                float z = spawnZ + Random.Range(-SpawnSpreadZ, SpawnSpreadZ);
                Vector3 pos = new Vector3(x, 1f, z);

                GameObject prefabToSpawn = PickEnemy(isCenterLane, wave.Intensity);
                if (prefabToSpawn == null) continue;

                GameObject go = EnemyPool.Instance != null
                    ? EnemyPool.Instance.Get(prefabToSpawn, pos, Quaternion.identity)
                    : Instantiate(prefabToSpawn, pos, Quaternion.identity);

                if (go == null) continue;

                EnemyCombatBase combat = go.GetComponent<EnemyCombatBase>();
                if (combat != null) combat.SetLane(lane);

                Enemy enemy = go.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.ApplyHealthMultiplier(wave.HealthMultiplier);
                    _aliveEnemyCount++;
                }
            }
        }
    }

    /// <summary>Спавнит сундук через EnemyPool — OnEnable оживит его и бар.</summary>
    private void SpawnChest(GateData chest)
    {
        if (chest.Prefab == null || EnemyPool.Instance == null) return;

        Vector3 pos = new Vector3(chest.X, chest.Prefab.transform.position.y, chest.Z);
        EnemyPool.Instance.Get(chest.Prefab, pos, chest.Prefab.transform.rotation);
    }

    private void SpawnGate(GateData data)
    {
        GameObject go = Instantiate(data.Prefab);
        go.transform.position = new Vector3(data.X, data.Prefab.transform.position.y, data.Z);
        go.transform.rotation = data.Prefab.transform.rotation;

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
        Debug.Log("[LevelGen] FinishLevel() вызван! Переход в Victory.");

        // Убираем оставшихся чейзящих/отступивших врагов при победе
        var list = EnemyCombatBase.AllEnemies;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null) list[i].DespawnToPool();
        }

        var launcher = LevelLauncher.Instance;
        var pdm      = PlayerDataManager.Instance;

        if (launcher != null && pdm != null && !string.IsNullOrEmpty(launcher.SelectedLevelId))
        {
            pdm.MarkLevelComplete(launcher.SelectedLevelId);
        }

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetVictory();

        // Показ UI Победы с вашей формы
        var victoryUI = FindAnyObjectByType<VictoryUI>(FindObjectsInactive.Include);
        if (victoryUI != null)
        {
            victoryUI.ForceShowVictory();
        }
        else
        {
            Debug.LogWarning("[LevelGen] VictoryUI не найден на игровой сцене! Пожалуйста, добавьте префаб/объект VictoryUI на Canvas игровой сцены.");
        }
    }

    /// <summary>
    /// Возвращает сколько врагов осталось убить спереди (живые спереди + ещё не заспавненные).
    /// Враги, прорвавшиеся назад (IsChasing / сзади лидера), не блокируют завершение уровня.
    /// </summary>
    public int GetTotalEnemiesRemaining()
    {
        int notSpawned = 0;
        if (_plan != null)
        {
            foreach (WaveData wave in _plan.Waves)
            {
                if (!wave.Spawned) notSpawned += wave.EnemiesPerLane * LaneSystem.LaneCount;
            }
        }

        int activeAhead = 0;
        var all = EnemyCombatBase.AllEnemies;
        float leaderZ = _leader != null ? _leader.position.z : 0f;

        for (int i = 0; i < all.Count; i++)
        {
            EnemyCombatBase combat = all[i];
            if (combat == null || !combat.gameObject.activeInHierarchy) continue;

            // Враги, бегущие сзади отряда (IsChasing или Z меньше лидера), не блокируют финиш
            if (combat.IsChasing || combat.transform.position.z < leaderZ) continue;

            Enemy e = combat.GetComponent<Enemy>();
            if (e != null && e.IsDead) continue;

            activeAhead++;
        }

        return activeAhead + notSpawned;
    }

    /// <summary>True если все запланированные волны уже заспавнены.</summary>
    private bool AllWavesSpawned()
    {
        if (_plan == null) return false;
        foreach (WaveData wave in _plan.Waves)
            if (!wave.Spawned) return false;
        return true;
    }

    /// <summary>ТЕСТ: спавнит сундук через пул в точке (x, z).</summary>
    private void SpawnChestAt(float x, float z)
    {
        if (_config.ChestPrefab == null || EnemyPool.Instance == null) return;

        Vector3 pos = new Vector3(x, _config.ChestPrefab.transform.position.y, z);
        EnemyPool.Instance.Get(_config.ChestPrefab, pos, _config.ChestPrefab.transform.rotation);
    }
}

