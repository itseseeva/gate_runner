using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул эффектов попадания снарядов. Никаких Instantiate в бою.
/// Эффект выбирается по стихии (Fire/Ice/Lightning/None).
/// Эффект возвращается в пул автоматически по таймеру _effectLifetime,
/// потому что система частиц сама не сообщает об окончании проигрывания.
/// </summary>
public class HitEffectPool : MonoBehaviour
{
    public static HitEffectPool Instance { get; private set; }

    [Header("Префабы эффектов по стихии")]
    [Tooltip("Эффект для нейтральных снарядов (ElementType.None)")]
    [SerializeField] private GameObject _hitNone;

    [Tooltip("Эффект для огненных снарядов")]
    [SerializeField] private GameObject _hitFire;

    [Tooltip("Эффект для ледяных снарядов")]
    [SerializeField] private GameObject _hitIce;

    [Tooltip("Эффект для молниевых снарядов")]
    [SerializeField] private GameObject _hitLightning;

    [Header("Настройки")]
    [Tooltip("Сколько эффектов каждой стихии создать заранее")]
    [SerializeField] private int _preloadCount = 10;

    [Tooltip("Через сколько секунд эффект возвращается в пул (примерная длительность партиклов)")]
    [SerializeField] private float _effectLifetime = 2f;

    // Каждая стихия имеет свою очередь готовых эффектов.
    private readonly Dictionary<ElementType, Queue<GameObject>> _pool = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        CreatePoolForElement(ElementType.None,      _hitNone);
        CreatePoolForElement(ElementType.Fire,      _hitFire);
        CreatePoolForElement(ElementType.Ice,       _hitIce);
        CreatePoolForElement(ElementType.Lightning, _hitLightning);

        Debug.Log("[HitEffectPool] Пул эффектов попадания готов!", this);
    }

    private void CreatePoolForElement(ElementType element, GameObject prefab)
    {
        // Если префаб не назначен — пропускаем эту стихию (не падаем).
        if (prefab == null)
        {
            Debug.LogWarning($"[HitEffectPool] Нет префаба для стихии {element} — эффект не будет проигрываться.", this);
            return;
        }

        _pool[element] = new Queue<GameObject>();

        for (int i = 0; i < _preloadCount; i++)
        {
            GameObject go = Instantiate(prefab, transform);
            go.SetActive(false);
            _pool[element].Enqueue(go);
        }
    }

    /// <summary>
    /// Проигрывает эффект попадания нужной стихии в указанной точке.
    /// Эффект сам вернётся в пул через _effectLifetime секунд.
    /// </summary>
    public void Play(ElementType element, Vector3 position)
    {
        Debug.Log($"[HitEffectPool] Play вызван, стихия={element}", this);

        // Если для стихии нет пула — пробуем нейтральный как запасной.
        if (!_pool.ContainsKey(element))
        {
            if (!_pool.ContainsKey(ElementType.None)) return;
            element = ElementType.None;
        }

        Queue<GameObject> queue = _pool[element];

        GameObject effect = queue.Count > 0 ? queue.Dequeue() : null;
        if (effect == null) return; // пул пуст в этот кадр — просто пропускаем эффект

        effect.transform.position = position;
        effect.SetActive(true);

        StartCoroutine(ReturnAfterDelay(element, effect));
    }

    private IEnumerator ReturnAfterDelay(ElementType element, GameObject effect)
    {
        yield return new WaitForSeconds(_effectLifetime);

        effect.SetActive(false);
        _pool[element].Enqueue(effect);
    }
}
