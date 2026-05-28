using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул VFX эффектов. Один на сцену.
/// Спавнит эффект, ждёт окончания Particle System, возвращает в пул.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _preloadCount = 10;

    private readonly Queue<ParticleSystem> _pool = new();
    
    // Пулы для разных prefab-ов
    private readonly Dictionary<GameObject, Queue<ParticleSystem>> _prefabPools = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        for (int i = 0; i < _preloadCount; i++)
        {
            ParticleSystem ps = CreateOne(_prefab);
            ps.gameObject.SetActive(false);
            _pool.Enqueue(ps);
        }
    }

    private ParticleSystem CreateOne(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.None;
        return ps;
    }

    /// <summary>Спавнит дефолтный VFX.</summary>
    public void Spawn(Vector3 position, Quaternion rotation)
    {
        SpawnFromPool(_pool, _prefab, position, rotation);
    }

    /// <summary>Спавнит конкретный VFX по prefab.</summary>
    public void Spawn(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        if (prefab == null)
        {
            Spawn(position, rotation);
            return;
        }

        if (!_prefabPools.ContainsKey(prefab))
            _prefabPools[prefab] = new Queue<ParticleSystem>();

        SpawnFromPool(_prefabPools[prefab], prefab, position, rotation);
    }

    private void SpawnFromPool(Queue<ParticleSystem> pool, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps;

        if (pool.Count > 0)
            ps = pool.Dequeue();
        else
            ps = CreateOne(prefab);

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
