using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит декор биома по бокам дороги. Декор появляется впереди,
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

    [Header("Спавн по Z")]
    [Tooltip("На каком Z впереди спавнить декор")]
    [SerializeField] private float _spawnZ = 100f;

    [Tooltip("За каким Z (позади) убирать декор в пул")]
    [SerializeField] private float _despawnZ = -15f;

    [Tooltip("Дистанция между рядами декора по Z")]
    [SerializeField] private float _spacingZ = 6f;

    [Header("Разброс")]
    [Tooltip("Случайный разброс масштаба: 0.25 = ±25%")]
    [SerializeField] private float _scaleVariation = 0.25f;

    // Активный декор в сцене — двигаем и проверяем каждый кадр.
    private readonly List<GameObject> _active = new();
    private float _nextSpawnZ;

    private void Awake()
    {
        // Защита: если в инспекторе сцены осталось старое значение < 80, поднимаем до 100
        if (_spawnZ < 80f) _spawnZ = 100f;
    }

    private void Start()
    {
        _nextSpawnZ = _spawnZ;
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

        // Идём от точки спавна назад к камере с тем же шагом, что в обычном спавне.
        for (float z = _spawnZ; z > _despawnZ; z -= _spacingZ)
        {
            SpawnOne(biome, leftSide: true,  atZ: z);
            SpawnOne(biome, leftSide: false, atZ: z);

            if (_spawnRoadDecor)
            {
                SpawnOnRoad(biome, leftSide: true,  atZ: z);
                SpawnOnRoad(biome, leftSide: false, atZ: z);
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
        // _nextSpawnZ едет назад вместе с миром. Когда доехал до spawnZ - spacingZ,
        // ставим новый ряд и отодвигаем точку вперёд.
        _nextSpawnZ -= WorldScroller.WorldSpeed * Time.deltaTime;

        if (_nextSpawnZ > _spawnZ - _spacingZ) return;
        _nextSpawnZ = _spawnZ;

        BiomeSO biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : null;
        if (biome == null) return;

        // Ставим по одному декору с каждой стороны дороги.
        SpawnOne(biome, leftSide: true);
        SpawnOne(biome, leftSide: false);

        // Декор на самой дороге — у краёв, чтобы не мешать геймплею в центре.
        if (_spawnRoadDecor)
        {
            SpawnOnRoad(biome, leftSide: true);
            SpawnOnRoad(biome, leftSide: false);
        }
    }

    private void SpawnOne(BiomeSO biome, bool leftSide, float atZ = float.NaN)
    {
        if (biome.Decor == null || biome.Decor.Length == 0) return;

        DecorEntry entry = biome.Decor[Random.Range(0, biome.Decor.Length)];
        if (entry.Prefab == null) return;

        float baseX = _roadHalfWidth + entry.SideMargin;
        float x = baseX + Random.Range(0f, _sideSpread);
        if (leftSide) x = -x;

        // Если Z не задан — спавним в стандартной точке впереди.
        float z = float.IsNaN(atZ) ? _spawnZ : atZ;
        Vector3 pos = new Vector3(x, 0f, z);

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        DisableColliders(go);

        float variation = 1f + Random.Range(-entry.ScaleVariation, entry.ScaleVariation);
        go.transform.localScale = Vector3.one * entry.Scale * variation;

        _active.Add(go);
    }

    private void SpawnOnRoad(BiomeSO biome, bool leftSide, float atZ = float.NaN)
    {
        DecorEntry[] list = biome.RoadDecor;
        if (list == null || list.Length == 0) return;

        DecorEntry entry = list[Random.Range(0, list.Length)];
        if (entry.Prefab == null) return;

        float x = _roadHalfWidth - _roadEdgeInset + Random.Range(-_roadChaosX, _roadChaosX);
        if (leftSide) x = -x;

        float z = float.IsNaN(atZ) ? _spawnZ : atZ;
        Vector3 pos = new Vector3(x, 0.01f, z);

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject go = DecorPool.Instance.Get(entry.Prefab, pos, rot);
        if (go == null) return;

        DisableColliders(go);

        float variation = 1f + Random.Range(-entry.ScaleVariation, entry.ScaleVariation);
        go.transform.localScale = Vector3.one * entry.Scale * variation;

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
