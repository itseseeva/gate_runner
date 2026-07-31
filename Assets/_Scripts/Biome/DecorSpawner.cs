using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит декор биома по бокам и на дороге. Декор появляется впереди,
/// едет назад вместе с миром, за камерой возвращается в пул.
/// Модели берутся из текущего биома (BiomeManager.CurrentBiome).
/// </summary>
public class DecorSpawner : MonoBehaviour
{
    [Header("Дорога")]
    [Tooltip("Полуширина дороги — за этот край ставим декор (2.5м при Scale X = 0.5)")]
    [SerializeField] private float _roadHalfWidth = 2.5f;

    [Header("Разброс вбок (Side Spread Min/Max)")]
    [Tooltip("Трава: минимальный отступ от края (метры)")]
    [SerializeField] private float _grassSideSpreadMin = 0.1f;
    [Tooltip("Трава: максимальный отступ от края (метры)")]
    [SerializeField] private float _grassSideSpreadMax = 2.5f;

    [Tooltip("Цветы: минимальный отступ от края (метры)")]
    [SerializeField] private float _flowerSideSpreadMin = 0.2f;
    [Tooltip("Цветы: максимальный отступ от края (метры)")]
    [SerializeField] private float _flowerSideSpreadMax = 2.5f;

    [Tooltip("Кусты: минимальный отступ от края (метры)")]
    [SerializeField] private float _bushSideSpreadMin = 0.3f;
    [Tooltip("Кусты: максимальный отступ от края (метры)")]
    [SerializeField] private float _bushSideSpreadMax = 4.5f;

    [Tooltip("Камни: минимальный отступ от края (метры)")]
    [SerializeField] private float _rockSideSpreadMin = 0.2f;
    [Tooltip("Камни: максимальный отступ от края (метры)")]
    [SerializeField] private float _rockSideSpreadMax = 4.0f;

    [Tooltip("Деревья: минимальный отступ от края (метры)")]
    [SerializeField] private float _treeSideSpreadMin = 1.5f;
    [Tooltip("Деревья: максимальный отступ от края (метры — глубина леса)")]
    [SerializeField] private float _treeSideSpreadMax = 9.0f;

    [Tooltip("Спавнить ли траву по бокам")]
    [SerializeField] private bool _spawnGrass = true;

    [Tooltip("Спавнить ли цветы по бокам")]
    [SerializeField] private bool _spawnFlowers = true;

    [Tooltip("Спавнить ли кусты по бокам")]
    [SerializeField] private bool _spawnBushes = true;

    [Tooltip("Спавнить ли камни по бокам")]
    [SerializeField] private bool _spawnRocks = true;

    [Tooltip("Спавнить ли деревья по бокам")]
    [SerializeField] private bool _spawnTrees = true;

    [Tooltip("Спавнить ли декор на дороге")]
    [SerializeField] private bool _spawnRoadDecor = true;

    [Header("Плотность")]
    [Tooltip("Сколько пучков травы ставить с каждой стороны за один ряд")]
    [Range(0, 10)]
    [SerializeField] private int _grassPerSide = 1;

    [Tooltip("Сколько цветов ставить с каждой стороны за один ряд")]
    [Range(0, 10)]
    [SerializeField] private int _flowersPerSide = 1;

    [Tooltip("Сколько кустов ставить с каждой стороны за один ряд")]
    [Range(0, 10)]
    [SerializeField] private int _bushesPerSide = 1;

    [Tooltip("Сколько камней ставить с каждой стороны за один ряд")]
    [Range(0, 10)]
    [SerializeField] private int _rocksPerSide = 1;

    [Tooltip("Сколько деревьев ставить с каждой стороны за один ряд")]
    [Range(0, 10)]
    [SerializeField] private int _treesPerSide = 1;

    [Tooltip("Сколько объектов дорожного декора за ряд с каждой стороны")]
    [Range(0, 8)]
    [SerializeField] private int _roadDecorPerSide = 1;

    [Tooltip("Разброс по Z внутри одного ряда — чтобы декор не стоял ровной шеренгой")]
    [SerializeField] private float _rowChaosZ = 2f;

    [Header("Спавн по Z")]
    [Tooltip("На каком Z впереди спавнить декор")]
    [SerializeField] private float _spawnZ = 70f;

    [Tooltip("За каким Z (позади) убирать декор в пул")]
    [SerializeField] private float _despawnZ = -15f;

    [Tooltip("Дистанция между рядами декора по Z")]
    [SerializeField] private float _spacingZ = 6f;


    // Активный декор в сцене — двигаем и проверяем каждый кадр.
    private readonly List<GameObject> _active = new();
    private float _nextSpawnZ;
    private float _nextFenceZ;

    private void Awake()
    {
        _spawnZ = 70f;
        _roadHalfWidth = 2.5f;
    }

    private void Start()
    {
        _nextSpawnZ = _spawnZ;
        _nextFenceZ = _spawnZ;
        PrewarmDecor();
    }

    /// <summary>
    /// Заполняет декором всю дистанцию от камеры до точки спавна при старте,
    /// чтобы мир не начинался пустым и не ждал, пока декор доедет издалека.
    /// </summary>
    private void PrewarmDecor()
    {
        if (_active.Count > 0) return;

        BiomeSO biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : null;
        if (biome == null) return;

        // Предзагружаем пул объектов для всего биома за один шаг
        if (DecorPool.Instance != null)
            DecorPool.Instance.PrewarmBiome(biome);

        // Забор идёт своим шагом — плотнее остального декора.
        if (biome.SpawnFence && biome.FenceSpacing > 0f)
        {
            for (float z = _spawnZ; z > _despawnZ; z -= biome.FenceSpacing)
                SpawnFence(biome, z);
        }

        // Идём от точки спавна назад к камере с тем же шагом, что в обычном спавне.
        for (float z = _spawnZ; z > _despawnZ; z -= _spacingZ)
        {
            if (_spawnGrass)
            {
                for (int i = 0; i < _grassPerSide; i++)
                {
                    SpawnOne(biome, biome.GrassDecor, leftSide: true,  minSpread: _grassSideSpreadMin, maxSpread: _grassSideSpreadMax, atZ: z);
                    SpawnOne(biome, biome.GrassDecor, leftSide: false, minSpread: _grassSideSpreadMin, maxSpread: _grassSideSpreadMax, atZ: z);
                }
            }

            if (_spawnFlowers)
            {
                for (int i = 0; i < _flowersPerSide; i++)
                {
                    SpawnOne(biome, biome.FlowerDecor, leftSide: true,  minSpread: _flowerSideSpreadMin, maxSpread: _flowerSideSpreadMax, atZ: z);
                    SpawnOne(biome, biome.FlowerDecor, leftSide: false, minSpread: _flowerSideSpreadMin, maxSpread: _flowerSideSpreadMax, atZ: z);
                }
            }

            if (_spawnBushes)
            {
                for (int i = 0; i < _bushesPerSide; i++)
                {
                    SpawnOne(biome, biome.BushDecor, leftSide: true,  minSpread: _bushSideSpreadMin, maxSpread: _bushSideSpreadMax, atZ: z);
                    SpawnOne(biome, biome.BushDecor, leftSide: false, minSpread: _bushSideSpreadMin, maxSpread: _bushSideSpreadMax, atZ: z);
                }
            }

            if (_spawnRocks)
            {
                for (int i = 0; i < _rocksPerSide; i++)
                {
                    SpawnOne(biome, biome.RockDecor, leftSide: true,  minSpread: _rockSideSpreadMin, maxSpread: _rockSideSpreadMax, atZ: z);
                    SpawnOne(biome, biome.RockDecor, leftSide: false, minSpread: _rockSideSpreadMin, maxSpread: _rockSideSpreadMax, atZ: z);
                }
            }

            if (_spawnTrees)
            {
                for (int i = 0; i < _treesPerSide; i++)
                {
                    SpawnOne(biome, biome.TreeDecor, leftSide: true,  minSpread: _treeSideSpreadMin, maxSpread: _treeSideSpreadMax, atZ: z);
                    SpawnOne(biome, biome.TreeDecor, leftSide: false, minSpread: _treeSideSpreadMin, maxSpread: _treeSideSpreadMax, atZ: z);
                }
            }

            if (_spawnRoadDecor)
            {
                for (int i = 0; i < _roadDecorPerSide; i++)
                {
                    SpawnOnRoad(biome, leftSide: true,  atZ: z);
                    SpawnOnRoad(biome, leftSide: false, atZ: z);
                }
            }
        }
    }

    private void Update()
    {
        // Если при старте биом ещё не успел загрузиться — пробуем предзаполнить в Update
        if (_active.Count == 0) PrewarmDecor();

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        MoveDecor();
        TrySpawn();
    }

    /// <summary>Двигает весь декор назад со скоростью мира, убирает ушедший за камеру.</summary>
    private void MoveDecor()
    {
        float worldSpeed = WorldScroller.WorldSpeed;
        float dz = worldSpeed * Time.deltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            GameObject go = _active[i];
            if (go == null) { _active.RemoveAt(i); continue; }

            go.transform.position += Vector3.back * dz;

            // Ушёл за камеру — в пул.
            if (go.transform.position.z < _despawnZ)
            {
                DecorPool.Instance.Return(go);
                _active.RemoveAt(i);
            }
        }
    }

    /// <summary>Спавнит новый ряд декора, когда мир проехал spacingZ.</summary>
    private void TrySpawn()
    {
        _nextSpawnZ -= WorldScroller.WorldSpeed * Time.deltaTime;
        _nextFenceZ -= WorldScroller.WorldSpeed * Time.deltaTime;

        BiomeSO biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : null;
        if (biome == null) return;

        // Забор — ровно каждые FenceSpacing метров без сдвига и накопления погрешности.
        if (biome.SpawnFence && biome.FenceSpacing > 0f && _nextFenceZ <= _spawnZ - biome.FenceSpacing)
        {
            _nextFenceZ += biome.FenceSpacing;
            SpawnFence(biome, _spawnZ);
        }

        // Остальной декор — по шагу spacingZ.
        if (_nextSpawnZ <= _spawnZ - _spacingZ)
        {
            _nextSpawnZ = _spawnZ;

            if (_spawnGrass)
            {
                for (int i = 0; i < _grassPerSide; i++)
                {
                    SpawnOne(biome, biome.GrassDecor, leftSide: true,  minSpread: _grassSideSpreadMin, maxSpread: _grassSideSpreadMax);
                    SpawnOne(biome, biome.GrassDecor, leftSide: false, minSpread: _grassSideSpreadMin, maxSpread: _grassSideSpreadMax);
                }
            }

            if (_spawnFlowers)
            {
                for (int i = 0; i < _flowersPerSide; i++)
                {
                    SpawnOne(biome, biome.FlowerDecor, leftSide: true,  minSpread: _flowerSideSpreadMin, maxSpread: _flowerSideSpreadMax);
                    SpawnOne(biome, biome.FlowerDecor, leftSide: false, minSpread: _flowerSideSpreadMin, maxSpread: _flowerSideSpreadMax);
                }
            }

            if (_spawnBushes)
            {
                for (int i = 0; i < _bushesPerSide; i++)
                {
                    SpawnOne(biome, biome.BushDecor, leftSide: true,  minSpread: _bushSideSpreadMin, maxSpread: _bushSideSpreadMax);
                    SpawnOne(biome, biome.BushDecor, leftSide: false, minSpread: _bushSideSpreadMin, maxSpread: _bushSideSpreadMax);
                }
            }

            if (_spawnRocks)
            {
                for (int i = 0; i < _rocksPerSide; i++)
                {
                    SpawnOne(biome, biome.RockDecor, leftSide: true,  minSpread: _rockSideSpreadMin, maxSpread: _rockSideSpreadMax);
                    SpawnOne(biome, biome.RockDecor, leftSide: false, minSpread: _rockSideSpreadMin, maxSpread: _rockSideSpreadMax);
                }
            }

            if (_spawnTrees)
            {
                for (int i = 0; i < _treesPerSide; i++)
                {
                    SpawnOne(biome, biome.TreeDecor, leftSide: true,  minSpread: _treeSideSpreadMin, maxSpread: _treeSideSpreadMax);
                    SpawnOne(biome, biome.TreeDecor, leftSide: false, minSpread: _treeSideSpreadMin, maxSpread: _treeSideSpreadMax);
                }
            }

            if (_spawnRoadDecor)
            {
                for (int i = 0; i < _roadDecorPerSide; i++)
                {
                    SpawnOnRoad(biome, leftSide: true);
                    SpawnOnRoad(biome, leftSide: false);
                }
            }
        }
    }

    /// <summary>
    /// Выбирает запись с учётом веса: чем больше Weight, тем чаще выпадает.
    /// Без этого добавление новых моделей размывало бы частоту старых.
    /// </summary>
    private DecorEntry PickWeighted(DecorEntry[] list)
    {
        if (list == null || list.Length == 0) return null;

        float total = 0f;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].Prefab == null) continue;
            total += Mathf.Max(0f, list[i].Weight);
        }

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].Prefab == null) continue;
            acc += Mathf.Max(0f, list[i].Weight);
            if (roll <= acc) return list[i];
        }

        return null;
    }

    private void SpawnOne(BiomeSO biome, DecorEntry[] list, bool leftSide, float minSpread, float maxSpread, float atZ = float.NaN)
    {
        DecorEntry entry = PickWeighted(list);
        if (entry == null || entry.Prefab == null) return;

        if (Random.value > entry.SpawnChance) return;

        float x = _roadHalfWidth + Random.Range(minSpread, maxSpread);
        if (leftSide) x = -x;

        float z = (float.IsNaN(atZ) ? _spawnZ : atZ) + entry.OffsetZ;
        // Забору разброс не нужен — иначе в линии появятся щели и нахлёсты.
        if (!entry.NoRandomRotation) z += Random.Range(-_rowChaosZ, _rowChaosZ);
        Vector3 pos = new Vector3(x, entry.OffsetY, z);

        // Забор и подобное не крутим — секции должны смотреть одинаково.
        Quaternion rot = entry.NoRandomRotation
            ? entry.Prefab.transform.rotation
            : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        float variation = 1f + Random.Range(-entry.ScaleVariation, entry.ScaleVariation);
        // Умножаем исходный масштаб префаба, а не заменяем — модели бывают
        // разного внутреннего размера, и Scale=1 должен давать размер как в префабе.
        go.transform.localScale = entry.Prefab.transform.localScale * entry.Scale * variation;

        _active.Add(go);
    }

    /// <summary>
    /// Спавнит декор на дороге. У краёв по умолчанию, но записи с галкой
    /// SpawnOnRoadCenter раскидываются по всей ширине.
    /// </summary>
    private void SpawnOnRoad(BiomeSO biome, bool leftSide, float atZ = float.NaN)
    {
        DecorEntry entry = PickWeighted(biome.RoadDecor);
        if (entry == null || entry.Prefab == null) return;

        if (Random.value > entry.SpawnChance) return;

        float x;
        if (entry.SpawnOnRoadCenter)
        {
            // По всей ширине дороги.
            float limit = Mathf.Max(0f, _roadHalfWidth);
            x = Random.Range(-limit, limit);
        }
        else
        {
            // У края дороги.
            x = _roadHalfWidth;
            if (leftSide) x = -x;
        }

        float z = (float.IsNaN(atZ) ? _spawnZ : atZ) + entry.OffsetZ;
        // Забору разброс не нужен — иначе в линии появятся щели и нахлёсты.
        if (!entry.NoRandomRotation) z += Random.Range(-_rowChaosZ, _rowChaosZ);

        Vector3 pos = new Vector3(x, 0.01f + entry.OffsetY, z);

        // Забор и подобное не крутим — секции должны смотреть одинаково.
        Quaternion rot = entry.NoRandomRotation
            ? entry.Prefab.transform.rotation
            : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        float variation = 1f + Random.Range(-entry.ScaleVariation, entry.ScaleVariation);
        go.transform.localScale = entry.Prefab.transform.localScale * entry.Scale * variation;

        _active.Add(go);
    }

    /// <summary>
    /// Ставит секцию забора с обеих сторон дороги. Отдельный поток со своим шагом —
    /// забор не конкурирует с травой и камнями за место в случайном выборе.
    /// </summary>
    private void SpawnFence(BiomeSO biome, float atZ)
    {
        if (biome.Fence == null || biome.Fence.Length == 0) return;

        float rightX = (_roadHalfWidth + biome.FenceOffset) - biome.RightFenceOffset;
        float leftX  = -(_roadHalfWidth + biome.FenceOffset) + biome.LeftFenceOffset;

        for (int i = 0; i < biome.Fence.Length; i++)
        {
            DecorEntry entry = biome.Fence[i];
            if (entry == null || entry.Prefab == null) continue;

            if (Random.value > entry.SpawnChance) continue;

            SpawnFenceSide(biome, entry, rightX, atZ);
            SpawnFenceSide(biome, entry, leftX, atZ);
        }
    }

    private void SpawnFenceSide(BiomeSO biome, DecorEntry entry, float x, float z)
    {
        Vector3 pos = new Vector3(x, biome.FenceOffsetY + entry.OffsetY, z + entry.OffsetZ);

        // Забор не крутим случайно — секции должны стоять ровно в линию.
        Quaternion rot = entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        // Масштаб без разброса — иначе секции разной высоты не состыкуются.
        go.transform.localScale = entry.Prefab.transform.localScale * entry.Scale;

        _active.Add(go);
    }
}
