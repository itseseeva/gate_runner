using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул врагов — по очереди на каждый префаб, как DecorPool/VfxPool.
/// Без него каждая волна делала Instantiate, а мёртвые враги
/// оставались в сцене выключенными и копились сотнями.
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Tooltip("Сколько врагов каждого типа создать заранее")]
    [SerializeField] private int _preloadCount = 20;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pool = new();

    private void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
    }

    /// <summary>Берёт врага из пула. Enemy.OnEnable сам восстановит его состояние.</summary>
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

        // Позицию ставим ДО активации — иначе OnEnable отработает в старой точке.
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go;
    }

    private GameObject CreateOne(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);

        var tag = go.GetComponent<PooledEnemy>();
        if (tag == null) tag = go.AddComponent<PooledEnemy>();
        tag.SourcePrefab = prefab;

        return go;
    }

    /// <summary>Возвращает врага в очередь его префаба.</summary>
    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);

        var tag = go.GetComponent<PooledEnemy>();
        if (tag == null || tag.SourcePrefab == null || !_pool.ContainsKey(tag.SourcePrefab))
        {
            Destroy(go);
            return;
        }

        _pool[tag.SourcePrefab].Enqueue(go);
    }
}

/// <summary>Метка на враге из пула — помнит, из какого префаба создан.</summary>
public class PooledEnemy : MonoBehaviour
{
    public GameObject SourcePrefab;
}
