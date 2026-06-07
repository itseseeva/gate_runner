using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул VFX эффектов. ОДИН на сцену.
/// Все конкретные эффекты приходят из VfxConfig — дефолтный prefab не нужен.
/// Спавнит эффект, ждёт окончания Particle System, возвращает в пул.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [Tooltip("Сколько экземпляров каждого эффекта создать заранее при первом использовании")]
    [SerializeField] private int _preloadCount = 5;

    // Отдельная очередь под каждый префаб эффекта
    private readonly Dictionary<GameObject, Queue<ParticleSystem>> _prefabPools = new();

    private void Awake()
    {
        // Singleton — один на сцену
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Спавнит конкретный VFX по префабу из VfxConfig.</summary>
    public void Spawn(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        if (prefab == null) return; // нет эффекта — ничего не делаем

        if (!_prefabPools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
        {
            pool = new Queue<ParticleSystem>();
            _prefabPools[prefab] = pool;

            // Прелоадим заранее чтобы не было Instantiate в первый удар
            for (int i = 0; i < _preloadCount; i++)
            {
                ParticleSystem preloaded = CreateOne(prefab);
                if (preloaded == null) break;
                preloaded.gameObject.SetActive(false);
                pool.Enqueue(preloaded);
            }
        }

        SpawnFromPool(pool, prefab, position, rotation);
    }

    private ParticleSystem CreateOne(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);

        // ParticleSystem может быть на корне ИЛИ на дочернем объекте.
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null) ps = go.GetComponentInChildren<ParticleSystem>();

        if (ps == null)
        {
            Debug.LogError($"[VfxPool] На префабе {prefab.name} нет ParticleSystem (ни на корне, ни в детях).", this);
            return null;
        }

        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.None;
        return ps;
    }

    private void SpawnFromPool(Queue<ParticleSystem> pool, GameObject prefab,
                               Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps = pool.Count > 0 ? pool.Dequeue() : CreateOne(prefab);
        if (ps == null) return; // префаб без ParticleSystem — не падаем

        ps.gameObject.SetActive(true);
        ps.transform.position = position;
        ps.transform.rotation = rotation;
        ps.Play();

        StartCoroutine(ReturnWhenDone(ps, pool));
    }

    private IEnumerator ReturnWhenDone(ParticleSystem ps, Queue<ParticleSystem> pool)
    {
        yield return new WaitUntil(() => !ps.IsAlive(true));
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }
}
