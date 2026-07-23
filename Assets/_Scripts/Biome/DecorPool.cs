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
    [SerializeField] private int _preloadCount = 8;

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

        // Запоминаем префаб-источник, чтобы вернуть в нужную очередь
        var tag = go.GetComponent<PooledDecor>();
        if (tag == null) tag = go.AddComponent<PooledDecor>();
        tag.SourcePrefab = prefab;

        return go;
    }

    private GameObject CreateOne(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);
        return go;
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
