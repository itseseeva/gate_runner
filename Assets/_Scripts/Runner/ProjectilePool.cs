using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Универсальный пул снарядов — очередь на каждый ПРЕФАБ (как VfxPool).
/// Префаб снаряда приходит от стрелка (из его HeroDefinitionSO),
/// поэтому маг и лучник используют разные снаряды, а пул переиспользует
/// любые без Instantiate в бою.
/// Снаряд помнит свой исходный префаб, чтобы вернуться в правильную очередь.
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [Header("Настройки")]
    [Tooltip("Сколько снарядов каждого префаба создать заранее при первом использовании")]
    [SerializeField] private int _preloadCount = 20;

    // Очередь готовых снарядов на каждый префаб.
    private readonly Dictionary<GameObject, Queue<Projectile>> _pool = new();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Берёт снаряд указанного префаба из пула (создаёт пул для префаба при первом обращении).
    /// </summary>
    public Projectile Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ProjectilePool] Запрошен снаряд с null-префабом.", this);
            return null;
        }

        // Первое обращение к этому префабу — создаём очередь и прелоадим.
        if (!_pool.TryGetValue(prefab, out Queue<Projectile> queue))
        {
            queue = new Queue<Projectile>();
            _pool[prefab] = queue;

            for (int i = 0; i < _preloadCount; i++)
            {
                Projectile pre = CreateProjectile(prefab);
                if (pre == null) break;
                pre.gameObject.SetActive(false);
                queue.Enqueue(pre);
            }
        }

        Projectile p = queue.Count > 0 ? queue.Dequeue() : CreateProjectile(prefab);
        if (p == null) return null;

        p.transform.position = position;
        p.transform.rotation = rotation;
        p.gameObject.SetActive(true);
        return p;
    }

    private Projectile CreateProjectile(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);
        Projectile p = go.GetComponent<Projectile>();
        if (p == null)
        {
            Debug.LogError($"[ProjectilePool] На префабе снаряда {prefab.name} нет компонента Projectile!", this);
            return null;
        }
        p.SourcePrefab = prefab; // запоминаем, в какую очередь возвращать
        return p;
    }

    /// <summary>Возвращает снаряд в очередь его префаба.</summary>
    public void Return(Projectile p)
    {
        if (p == null) return;

        p.gameObject.SetActive(false);

        GameObject prefab = p.SourcePrefab;
        if (prefab == null || !_pool.ContainsKey(prefab))
        {
            // Источник неизвестен (не должно случаться) — уничтожаем, чтобы не копить мусор.
            Destroy(p.gameObject);
            return;
        }

        _pool[prefab].Enqueue(p);
    }
}