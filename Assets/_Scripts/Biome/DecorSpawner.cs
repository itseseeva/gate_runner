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
    [Tooltip("Полуширина дороги — за этот край ставим декор")]
    [SerializeField] private float _roadHalfWidth = 2.5f;

    [Tooltip("Отступ декора от края дороги (метры)")]
    [SerializeField] private float _sideMargin = 1.5f;

    [Tooltip("Разброс декора вбок за краем (метры)")]
    [SerializeField] private float _sideSpread = 4f;

    [Header("Декор на дороге")]
    [Tooltip("Отступ от края дороги внутрь — насколько глубоко залезает декор")]
    [SerializeField] private float _roadEdgeInset = 0.3f;

    [Tooltip("Хаос позиции по X на дороге")]
    [SerializeField] private float _roadChaosX = 0.4f;

    [Tooltip("Спавнить ли декор на дороге")]
    [SerializeField] private bool _spawnRoadDecor = true;

    [Header("Плотность")]
    [Tooltip("Сколько объектов декора ставить с каждой стороны за один ряд")]
    [Range(1, 10)]
    [SerializeField] private int _decorPerSide = 1;

    [Tooltip("Сколько объектов дорожного декора за ряд с каждой стороны")]
    [Range(1, 8)]
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

    [Header("Разброс")]
    [Tooltip("Случайный разброс масштаба: 0.25 = ±25%")]
    [SerializeField] private float _scaleVariation = 0.25f;

    [Header("Забор")]
    [Tooltip("Ставить ли забор вдоль дороги")]
    [SerializeField] private bool _spawnFence = true;

    [Tooltip("Длина секции забора по Z — шаг между секциями. Должен совпадать с реальной длиной модели.")]
    [SerializeField] private float _fenceSpacing = 2f;

    [Tooltip("Отступ забора от края дороги")]
    [SerializeField] private float _fenceOffset = 0.2f;

    // Активный декор в сцене — двигаем и проверяем каждый кадр.
    private readonly List<GameObject> _active = new();
    private float _nextSpawnZ;
    private float _nextFenceZ;

    private void Awake()
    {
        // Защита: если в инспекторе сцены осталось значение < 50, устанавливаем 70
        if (_spawnZ < 50f) _spawnZ = 70f;
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

        // Забор идёт своим шагом — плотнее остального декора.
        if (_spawnFence)
        {
            for (float z = _spawnZ; z > _despawnZ; z -= _fenceSpacing)
                SpawnFence(biome, z);
        }

        // Идём от точки спавна назад к камере с тем же шагом, что в обычном спавне.
        for (float z = _spawnZ; z > _despawnZ; z -= _spacingZ)
        {
            for (int i = 0; i < _decorPerSide; i++)
            {
                SpawnOne(biome, leftSide: true,  atZ: z);
                SpawnOne(biome, leftSide: false, atZ: z);
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

        // Забор — свой шаг, чтобы секции стыковались без щелей.
        if (_spawnFence && _nextFenceZ <= _spawnZ - _fenceSpacing)
        {
            _nextFenceZ = _spawnZ;
            SpawnFence(biome, _spawnZ);
        }

        // Остальной декор — как было.
        if (_nextSpawnZ > _spawnZ - _spacingZ) return;
        _nextSpawnZ = _spawnZ;

        for (int i = 0; i < _decorPerSide; i++)
        {
            SpawnOne(biome, leftSide: true);
            SpawnOne(biome, leftSide: false);
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

    private void SpawnOne(BiomeSO biome, bool leftSide, float atZ = float.NaN)
    {
        DecorEntry entry = PickWeighted(biome.Decor);
        if (entry == null || entry.Prefab == null) return;

        float baseX = _roadHalfWidth + entry.SideMargin;
        float x = baseX + Random.Range(0f, _sideSpread);
        if (leftSide) x = -x;

        float z = float.IsNaN(atZ) ? _spawnZ : atZ;
        // Забору разброс не нужен — иначе в линии появятся щели и нахлёсты.
        if (!entry.NoRandomRotation) z += Random.Range(-_rowChaosZ, _rowChaosZ);
        Vector3 pos = new Vector3(x, 0f, z);

        // Забор и подобное не крутим — секции должны смотреть одинаково.
        Quaternion rot = entry.NoRandomRotation
            ? entry.Prefab.transform.rotation
            : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        DisableColliders(go);

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

        // Своя вероятность у каждой записи — редкие вещи ставятся не всегда.
        if (Random.value > entry.SpawnChance) return;

        float x;
        if (entry.SpawnOnRoadCenter)
        {
            // По всей ширине дороги, с отступом от краёв.
            float limit = _roadHalfWidth - _roadEdgeInset;
            x = Random.Range(-limit, limit);
        }
        else
        {
            // У края, как раньше.
            x = _roadHalfWidth - _roadEdgeInset + Random.Range(-_roadChaosX, _roadChaosX);
            if (leftSide) x = -x;
        }

        float z = float.IsNaN(atZ) ? _spawnZ : atZ;
        // Забору разброс не нужен — иначе в линии появятся щели и нахлёсты.
        if (!entry.NoRandomRotation) z += Random.Range(-_rowChaosZ, _rowChaosZ);

        Vector3 pos = new Vector3(x, 0.01f, z);

        // Забор и подобное не крутим — секции должны смотреть одинаково.
        Quaternion rot = entry.NoRandomRotation
            ? entry.Prefab.transform.rotation
            : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        DisableColliders(go);

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
        DecorEntry entry = biome.Fence;
        if (entry == null || entry.Prefab == null) return;

        float x = _roadHalfWidth + _fenceOffset;

        SpawnFenceSide(entry, x, atZ);
        SpawnFenceSide(entry, -x, atZ);
    }

    private void SpawnFenceSide(DecorEntry entry, float x, float z)
    {
        Vector3 pos = new Vector3(x, 0f, z);

        // Забор не крутим случайно — секции должны стоять ровно в линию.
        Quaternion rot = entry.Prefab.transform.rotation;

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        DisableColliders(go);

        // Масштаб без разброса — иначе секции разной высоты не состыкуются.
        go.transform.localScale = entry.Prefab.transform.localScale * entry.Scale;

        _active.Add(go);
    }

    /// <summary>Выключает все коллайдеры на декоре и его детях.</summary>
    private void DisableColliders(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
            c.enabled = false;
    }
}
