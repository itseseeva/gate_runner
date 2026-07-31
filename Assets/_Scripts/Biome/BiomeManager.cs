using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Один биом: имя, данные (BiomeSO для декора) и префабы трёх дорог.
/// [System.Serializable] позволяет Unity показать этот класс блоком в Inspector.
/// </summary>
[System.Serializable]
public class BiomeRoadSet
{
    [Tooltip("Название биома для удобства (Forest, Desert...)")]
    public string Name = "Biome";

    [Tooltip("Данные биома: декор (трава, деревья, камни, забор). Нужно для DecorSpawner.")]
    public BiomeSO Data;

    [Tooltip("Префаб центральной дороги")]
    public GameObject RoadCenter;

    [Tooltip("Префаб левой обочины")]
    public GameObject RoadLeft;

    [Tooltip("Префаб правой обочины")]
    public GameObject RoadRight;
}

/// <summary>
/// Собирает биом: спавнит префабы дорог и отдаёт данные декора текущего биома.
/// Хранит несколько биомов списком, переключается по индексу —
/// удобно тестировать биомы прямо в редакторе.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("Биомы (добавляй сюда через +)")]
    [Tooltip("Список всех биомов. Каждый хранит данные декора и префабы дорог.")]
    [SerializeField] private BiomeRoadSet[] _biomes;

    [Header("Какой биом показать")]
    [Tooltip("Индекс биома из списка выше: 0 = первый, 1 = второй...")]
    [SerializeField] private int _currentBiomeIndex = 0;

    [Header("Куда спавнить дороги")]
    [Tooltip("Родитель для заспавненных дорог. Если пусто — спавнит в корень сцены.")]
    [SerializeField] private Transform _roadParent;

    // Заспавненные сейчас дороги — чтобы удалить их при смене биома.
    private readonly List<GameObject> _spawnedRoads = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        SpawnBiome(_currentBiomeIndex);
    }

    /// <summary>Удаляет текущие дороги и спавнит дороги биома по индексу.</summary>
    public void SpawnBiome(int index)
    {
        if (_biomes == null || _biomes.Length == 0)
        {
            Debug.LogWarning("[BiomeManager] Список биомов пуст.", this);
            return;
        }

        if (index < 0 || index >= _biomes.Length)
        {
            Debug.LogWarning($"[BiomeManager] Индекс биома {index} вне списка (0..{_biomes.Length - 1}).", this);
            return;
        }

        ClearSpawnedRoads();

        _currentBiomeIndex = index;
        BiomeRoadSet biome = _biomes[index];

        SpawnRoad(biome.RoadCenter);
        SpawnRoad(biome.RoadLeft);
        SpawnRoad(biome.RoadRight);

        Debug.Log($"[BiomeManager] Биом '{biome.Name}' собран.", this);
    }

    /// <summary>Спавнит один префаб дороги, если он задан.</summary>
    private void SpawnRoad(GameObject prefab)
    {
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, _roadParent);
        _spawnedRoads.Add(instance);
    }

    /// <summary>Удаляет все ранее заспавненные дороги.</summary>
    private void ClearSpawnedRoads()
    {
        foreach (GameObject road in _spawnedRoads)
        {
            if (road != null)
                Destroy(road);
        }
        _spawnedRoads.Clear();
    }

    /// <summary>
    /// Данные текущего биома (декор: трава, деревья, забор).
    /// DecorSpawner берёт отсюда модели для спавна.
    /// </summary>
    public BiomeSO CurrentBiome =>
        (_biomes != null && _currentBiomeIndex >= 0 && _currentBiomeIndex < _biomes.Length)
            ? _biomes[_currentBiomeIndex].Data
            : null;

    /// <summary>Имя текущего биома.</summary>
    public string CurrentBiomeName =>
        (_biomes != null && _currentBiomeIndex >= 0 && _currentBiomeIndex < _biomes.Length)
            ? _biomes[_currentBiomeIndex].Name
            : "None";
}
