using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул взрывов смерти врага, разделённый по стихии (None/Fire/Ice/Lightning).
/// Никаких Instantiate в бою — всё прелоадено заранее.
/// Логика 1-в-1 как HitEffectPool, но с перезапуском партиклов при выдаче из пула.
/// Взрыв выбирается по стихии, которой враг был убит.
/// Эффект возвращается в пул автоматически по таймеру _effectLifetime.
/// </summary>
public class ExplosionPool : MonoBehaviour
{
    public static ExplosionPool Instance { get; private set; }

    [Header("Префабы взрывов по стихии")]
    [Tooltip("Взрыв для смерти без стихии (ElementType.None)")]
    [SerializeField] private GameObject _explosionNone;

    [Tooltip("Огненный взрыв")]
    [SerializeField] private GameObject _explosionFire;

    [Tooltip("Ледяной взрыв")]
    [SerializeField] private GameObject _explosionIce;

    [Tooltip("Молниевый взрыв")]
    [SerializeField] private GameObject _explosionLightning;

    [Header("Настройки")]
    [Tooltip("Сколько взрывов каждой стихии создать заранее")]
    [SerializeField] private int _preloadCount = 10;

    [Tooltip("Через сколько секунд взрыв возвращается в пул (примерная длительность партиклов)")]
    [SerializeField] private float _effectLifetime = 2f;

    // Каждая стихия имеет свою очередь готовых взрывов.
    private readonly Dictionary<ElementType, Queue<GameObject>> _pool = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        CreatePoolForElement(ElementType.None,      _explosionNone);
        CreatePoolForElement(ElementType.Fire,      _explosionFire);
        CreatePoolForElement(ElementType.Ice,       _explosionIce);
        CreatePoolForElement(ElementType.Lightning, _explosionLightning);

        Debug.Log("[ExplosionPool] Пул взрывов готов!", this);
    }

    private void CreatePoolForElement(ElementType element, GameObject prefab)
    {
        // Если префаб не назначен — пропускаем эту стихию (не падаем).
        if (prefab == null)
        {
            Debug.LogWarning($"[ExplosionPool] Нет префаба взрыва для стихии {element} — взрыв этой стихии не будет проигрываться.", this);
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
    /// Проигрывает взрыв нужной стихии в указанной точке.
    /// Эффект сам вернётся в пул через _effectLifetime секунд.
    /// Если для стихии нет пула — пробует нейтральный как запасной.
    /// </summary>
    public void Play(ElementType element, Vector3 position)
    {
        // Если для стихии нет пула — fallback на нейтральный.
        if (!_pool.ContainsKey(element))
        {
            if (!_pool.ContainsKey(ElementType.None)) return;
            element = ElementType.None;
        }

        Queue<GameObject> queue = _pool[element];

        GameObject effect = queue.Count > 0 ? queue.Dequeue() : null;
        if (effect == null) return; // пул пуст в этот кадр — просто пропускаем

        effect.transform.position = position;
        effect.SetActive(true);

        // Перезапускаем партиклы — из пула они сами не стартуют заново.
        RestartParticles(effect);

        StartCoroutine(ReturnAfterDelay(element, effect));
    }

    private void RestartParticles(GameObject effect)
    {
        PlayDesynced(effect);
    }

    private void PlayDesynced(GameObject effect)
    {
        ParticleSystem root = effect.GetComponent<ParticleSystem>();
        if (root == null) root = effect.GetComponentInChildren<ParticleSystem>();
        if (root == null) return;

        // Случайная стартовая фаза в пределах цикла эффекта.
        float phase = Random.Range(0f, root.main.duration);

        // Промотка на phase секунд вперёд и продолжение с этого места.
        // withChildren:true — двигаем все дочерние партиклы согласованно.
        root.Simulate(phase, true, true, false);
        root.Play(true);
    }

    private IEnumerator ReturnAfterDelay(ElementType element, GameObject effect)
    {
        yield return new WaitForSeconds(_effectLifetime);

        effect.SetActive(false);
        _pool[element].Enqueue(effect);
    }
}