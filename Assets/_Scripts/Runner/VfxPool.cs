using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул VFX эффектов. ОДИН на сцену.
/// Единица пула — корневой инстанс эффекта (GameObject), а не ParticleSystem.
/// Scale из корня префаба умножается на оригинальный startSizeMultiplier
/// каждой PS — так художник полностью контролирует размер из инспектора.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [Tooltip("Сколько экземпляров каждого эффекта создать заранее при первом использовании")]
    [SerializeField] private int _preloadCount = 5;

    private class VfxInstance
    {
        public GameObject     Root;          // корневой инстанс
        public ParticleSystem Particles;     // главные партиклы (для IsAlive/Clear)
        public ParticleSystem[] AllSystems;  // все PS внутри эффекта
        public float[] OriginalSizes;        // startSizeMultiplier как в префабе
    }

    private readonly Dictionary<GameObject, Queue<VfxInstance>> _prefabPools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Публичные методы Spawn ─────────────────────────────────────────────

    /// <summary>Спавнит VFX, беря ротацию и scale прямо с корня префаба.</summary>
    public GameObject Spawn(Vector3 position, GameObject prefab)
    {
        return Spawn(position, prefab.transform.rotation, prefab);
    }

    /// <summary>Спавнит VFX с произвольной ротацией. Scale берётся с корня префаба.</summary>
    public GameObject Spawn(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        if (prefab == null) return null;

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

        return SpawnFromPool(pool, prefab, position, rotation);
    }

    /// <summary>Спавнит VFX с позицией/ротацией/scale из самого префаба.</summary>
    public GameObject SpawnAtPrefabTransform(GameObject prefab)
    {
        if (prefab == null) return null;
        return Spawn(prefab.transform.position, prefab.transform.rotation, prefab);
    }

    // ── Внутренние методы ──────────────────────────────────────────────────

    private VfxInstance CreateOne(GameObject prefab)
    {
        GameObject root = Instantiate(prefab, transform);

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

        // Запоминаем оригинальные размеры всех PS сразу после Instantiate,
        // пока они ещё чистые (1:1 из префаба).
        ParticleSystem[] allSystems = root.GetComponentsInChildren<ParticleSystem>(true);
        float[] originalSizes = new float[allSystems.Length];
        for (int i = 0; i < allSystems.Length; i++)
            originalSizes[i] = allSystems[i].main.startSizeMultiplier;

        return new VfxInstance
        {
            Root          = root,
            Particles     = ps,
            AllSystems    = allSystems,
            OriginalSizes = originalSizes,
        };
    }

    private GameObject SpawnFromPool(Queue<VfxInstance> pool, GameObject prefab,
                                     Vector3 position, Quaternion rotation)
    {
        VfxInstance inst = pool.Count > 0 ? pool.Dequeue() : CreateOne(prefab);
        if (inst == null) return null;

        inst.Root.transform.SetParent(transform);
        inst.Root.transform.position   = position;
        inst.Root.transform.rotation   = rotation;
        inst.Root.transform.localScale = Vector3.one;

        // Scale из префаба умножаем на оригинальный startSizeMultiplier каждой PS.
        // Так настройки художника сохраняются, а scale просто масштабирует их.
        float scaleMul = prefab.transform.lossyScale.x;
        Debug.Log($"[VfxPool] Spawn {prefab.name}: lossyScale={prefab.transform.lossyScale}, scaleMul={scaleMul}", prefab);
        for (int i = 0; i < inst.AllSystems.Length; i++)
        {
            var m = inst.AllSystems[i].main;
            m.startSizeMultiplier = inst.OriginalSizes[i] * scaleMul;
        }

        inst.Root.SetActive(true);
        inst.Particles.Clear(true);

        foreach (var ps in inst.AllSystems)
            ps.Play(true);

        StartCoroutine(ReturnWhenDone(inst, pool));
        return inst.Root;
    }

    private IEnumerator ReturnWhenDone(VfxInstance inst, Queue<VfxInstance> pool)
    {
        yield return new WaitUntil(() => !inst.Particles.IsAlive(true));
        inst.Root.SetActive(false);
        pool.Enqueue(inst);
    }
}