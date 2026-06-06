using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул снарядов, разделённый по стихии (None/Fire/Ice/Lightning).
/// Никаких Instantiate в бою — всё преложено.
/// Каждая стихия имеет свой префаб снаряда (со своим трейлом) и свою очередь.
/// Снаряд возвращается в очередь той же стихии, с которой был выпущен.
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [Header("Префабы снарядов по стихии")]
    [Tooltip("Снаряд для нейтральных стрелков (ElementType.None)")]
    [SerializeField] private GameObject _projectileNone;

    [Tooltip("Огненный снаряд")]
    [SerializeField] private GameObject _projectileFire;

    [Tooltip("Ледяной снаряд")]
    [SerializeField] private GameObject _projectileIce;

    [Tooltip("Молниевый снаряд")]
    [SerializeField] private GameObject _projectileLightning;

    [Header("Настройки")]
    [Tooltip("Сколько снарядов каждой стихии создать заранее")]
    [SerializeField] private int _preloadCount = 30;

    // Каждая стихия имеет свою очередь готовых снарядов.
    private readonly Dictionary<ElementType, Queue<Projectile>> _pool = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        CreatePoolForElement(ElementType.None,      _projectileNone);
        CreatePoolForElement(ElementType.Fire,      _projectileFire);
        CreatePoolForElement(ElementType.Ice,       _projectileIce);
        CreatePoolForElement(ElementType.Lightning, _projectileLightning);

        Debug.Log("[ProjectilePool] Пул снарядов готов!", this);
    }

    private void CreatePoolForElement(ElementType element, GameObject prefab)
    {
        // Если префаб не назначен — пропускаем стихию (не падаем).
        if (prefab == null)
        {
            Debug.LogWarning($"[ProjectilePool] Нет префаба снаряда для стихии {element} — снаряды этой стихии не будут вылетать.", this);
            return;
        }

        _pool[element] = new Queue<Projectile>();

        for (int i = 0; i < _preloadCount; i++)
        {
            Projectile p = CreateProjectile(prefab, element);
            p.gameObject.SetActive(false);
            _pool[element].Enqueue(p);
        }
    }

    private Projectile CreateProjectile(GameObject prefab, ElementType element)
    {
        GameObject go = Instantiate(prefab, transform);
        Projectile p = go.GetComponent<Projectile>();
        if (p == null)
        {
            Debug.LogError($"[ProjectilePool] На префабе снаряда {prefab.name} нет компонента Projectile!", this);
        }
        return p;
    }

    /// <summary>
    /// Берёт снаряд нужной стихии из пула и ставит в позицию стрелка.
    /// Если для стихии нет пула — пробует нейтральный как запасной.
    /// </summary>
    public Projectile Get(ElementType element, Vector3 position, Quaternion rotation)
    {
        // Нет пула для стихии — fallback на нейтральный.
        if (!_pool.ContainsKey(element))
        {
            if (!_pool.ContainsKey(ElementType.None)) return null;
            element = ElementType.None;
        }

        Queue<Projectile> queue = _pool[element];

        Projectile p = queue.Count > 0 ? queue.Dequeue() : null;
        if (p == null) return null; // пул пуст в этот кадр

        p.transform.position = position;
        p.transform.rotation = rotation;
        p.gameObject.SetActive(true);
        return p;
    }

    /// <summary>
    /// Возвращает снаряд в очередь его стихии.
    /// Стихию берём у самого снаряда — он помнит её после Launch.
    /// </summary>
    public void Return(Projectile p)
    {
        if (p == null) return;

        p.gameObject.SetActive(false);

        ElementType element = p.Element;

        // Если очереди для стихии нет — кладём в нейтральную, чтобы не потерять снаряд.
        if (!_pool.ContainsKey(element))
        {
            if (!_pool.ContainsKey(ElementType.None)) return;
            element = ElementType.None;
        }

        _pool[element].Enqueue(p);
    }
}
