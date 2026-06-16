using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул VFX эффектов. ОДИН на сцену.
/// Единица пула — корневой инстанс эффекта (GameObject), а не ParticleSystem.
/// Это позволяет корректно ставить позицию эффекта независимо от того,
/// лежит ParticleSystem на корне или на дочернем объекте.
/// Спавнит эффект, ждёт окончания всех частиц, возвращает в пул.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [Tooltip("Сколько экземпляров каждого эффекта создать заранее при первом использовании")]
    [SerializeField] private int _preloadCount = 5;

    /// <summary>Один экземпляр эффекта в пуле: корень + его ParticleSystem.</summary>
    private class VfxInstance
    {
        public GameObject Root;          // корневой инстанс — его двигаем и включаем
        public ParticleSystem Particles; // партиклы — для Play / проверки завершения
    }

    // Очередь готовых инстансов на каждый префаб.
    private readonly Dictionary<GameObject, Queue<VfxInstance>> _prefabPools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Спавнит конкретный VFX по префабу в указанной позиции.</summary>
    public void Spawn(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        if (prefab == null) return;

        if (!_prefabPools.TryGetValue(prefab, out Queue<VfxInstance> pool))
        {
            pool = new Queue<VfxInstance>();
            _prefabPools[prefab] = pool;

            for (int i = 0; i < _preloadCount; i++)
            {
                VfxInstance preloaded = CreateOne(prefab);
                if (preloaded == null) break;
                preloaded.Root.SetActive(false);
                pool.Enqueue(preloaded);
            }
        }

        SpawnFromPool(pool, prefab, position, rotation);
    }

    private VfxInstance CreateOne(GameObject prefab)
    {
        GameObject root = Instantiate(prefab, transform);

        // ParticleSystem может быть на корне или на дочернем объекте.
        ParticleSystem ps = root.GetComponent<ParticleSystem>();
        if (ps == null) ps = root.GetComponentInChildren<ParticleSystem>();

        if (ps == null)
        {
            Debug.LogError($"[VfxPool] На префабе {prefab.name} нет ParticleSystem.", this);
            Destroy(root);
            return null;
        }

        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.None;

        return new VfxInstance { Root = root, Particles = ps };
    }

    private void SpawnFromPool(Queue<VfxInstance> pool, GameObject prefab,
                               Vector3 position, Quaternion rotation)
    {
        VfxInstance inst = pool.Count > 0 ? pool.Dequeue() : CreateOne(prefab);
        if (inst == null) return;

        // Двигаем КОРЕНЬ — позиция эффекта = точка попадания, без смещений.
        inst.Root.transform.position = position;
        inst.Root.transform.rotation = rotation;
        inst.Root.transform.localScale = prefab.transform.localScale; // ← scale с префаба
        inst.Root.SetActive(true);

        inst.Particles.Clear(true); // сбрасываем хвост от прошлого использования

        // Запускаем все партиклы эффекта с начала (muzzle/hit играют как задумано)
        ParticleSystem[] systems = inst.Root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
            ps.Play(true);

        StartCoroutine(ReturnWhenDone(inst, pool));
    }

    private IEnumerator ReturnWhenDone(VfxInstance inst, Queue<VfxInstance> pool)
    {
        yield return new WaitUntil(() => !inst.Particles.IsAlive(true));
        inst.Root.SetActive(false);
        pool.Enqueue(inst);
    }
}