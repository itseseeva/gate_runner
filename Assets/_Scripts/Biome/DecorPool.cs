using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул декораций — очередь на каждый префаб, как VfxPool/ProjectilePool.
/// Декор переиспользуется без Instantiate в игре: уехал за камеру → вернулся в пул.
/// </summary>
public class DecorPool : MonoBehaviour
{
    public static DecorPool Instance { get; private set; }

    [Tooltip("Сколько экземпляров каждого префаба создать заранее")]
    [SerializeField] private int _preloadCount = 20;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pool = new();

    private void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>Берёт декор из пула (создаёт очередь при первом обращении к префабу).</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_pool.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pool[prefab] = queue;

            for (int i = 0; i < _preloadCount; i++)
            {
                GameObject pre = CreateOne(prefab);
                pre.SetActive(false);
                queue.Enqueue(pre);
            }
        }

        GameObject go = queue.Count > 0 ? queue.Dequeue() : CreateOne(prefab);

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go;
    }

    /// <summary>Создаёт один экземпляр префаба, заранее повесив PooledDecor и удалив коллайдеры.</summary>
    private GameObject CreateOne(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);

        // Вешаем метку пула сразу при создании (без GC аллокаций во время Get)
        var tag = go.AddComponent<PooledDecor>();
        tag.SourcePrefab = prefab;

        // Удаляем физические коллайдеры один раз при создании экземпляра, а не во время игры
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                Destroy(colliders[i]);
        }

        return go;
    }

    /// <summary>Предварительно заполняет пул для префаба заданной выемкой.</summary>
    public void PrewarmPrefab(GameObject prefab, int count = -1)
    {
        if (prefab == null) return;
        if (count <= 0) count = _preloadCount;

        if (!_pool.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pool[prefab] = queue;
        }

        int toCreate = count - queue.Count;
        for (int i = 0; i < toCreate; i++)
        {
            GameObject pre = CreateOne(prefab);
            pre.SetActive(false);
            queue.Enqueue(pre);
        }
    }

    /// <summary>Предзагружает все префабы декораций текущего биома.</summary>
    public void PrewarmBiome(BiomeSO biome)
    {
        if (biome == null) return;

        PrewarmEntries(biome.GrassDecor);
        PrewarmEntries(biome.FlowerDecor);
        PrewarmEntries(biome.BushDecor);
        PrewarmEntries(biome.RockDecor);
        PrewarmEntries(biome.TreeDecor);
        PrewarmEntries(biome.RoadDecor);
        if (biome.Fence != null && biome.Fence.Prefab != null)
            PrewarmPrefab(biome.Fence.Prefab, _preloadCount * 2);
    }

    private void PrewarmEntries(DecorEntry[] entries)
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].Prefab != null)
                PrewarmPrefab(entries[i].Prefab);
        }
    }

    /// <summary>Возвращает декор в очередь его префаба.</summary>
    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);

        var tag = go.GetComponent<PooledDecor>();
        if (tag == null || tag.SourcePrefab == null || !_pool.ContainsKey(tag.SourcePrefab))
        {
            Destroy(go);
            return;
        }

        _pool[tag.SourcePrefab].Enqueue(go);
    }
}

/// <summary>Метка на заспавненном декоре — помнит, из какого префаба создан.</summary>
public class PooledDecor : MonoBehaviour
{
    public GameObject SourcePrefab;
}
