using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Хранит активные статусы на враге, тикает их и применяет эффекты.
/// Висит на каждом враге.
///
/// Архитектура:
/// - Каждый статус имеет время истечения (expiry time).
/// - Если статус уже есть и накладывают тот же — обновляется expiry (не стакается).
/// - При истечении статус снимается, эффект убирается.
/// - Burning тикает уроном раз в секунду пока активен.
/// - Frozen меняет SpeedMultiplier у WorldScroller врага.
/// - Shocked — флаг для DamageCalculator.
///
/// Визуал статусов:
/// - В слоты кидается ПРЕФАБ эффекта (Burning/Frozen/Shocked).
/// - Скрипт по имени префаба находит дочерний объект-эффект внутри врага
///   (например префаб FireAura → дочерний "FireAura") и включает/выключает его.
/// - Эффект создаётся лениво из префаба при первом наложении статуса и переиспользуется.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class StatusController : MonoBehaviour
{
    [Header("Префабы эффектов — по имени ищется дочерний объект врага")]
    [Tooltip("Префаб эффекта горения. Скрипт найдёт дочерний объект с таким же именем.")]
    [SerializeField] private GameObject _burningPrefab;

    [Tooltip("Префаб эффекта заморозки.")]
    [SerializeField] private GameObject _frozenPrefab;

    [Tooltip("Префаб эффекта электризации.")]
    [SerializeField] private GameObject _shockedPrefab;

    private Enemy         _enemy;
    private WorldScroller _scroller;

    // Созданные экземпляры эффектов (инстанцируются один раз из префаба, переиспользуются)
    private GameObject _burningEffect;
    private GameObject _frozenEffect;
    private GameObject _shockedEffect;

    // ─── Активные статусы ────────────────────────────────────────
    // Ключ = тип статуса, Значение = время когда статус истекает (Time.time)
    private readonly Dictionary<StatusEffectType, float> _expiryTime = new();

    // Для Burning: сколько урона тикать и время следующего тика
    private int   _burnDamagePerTick;
    private float _nextBurnTickTime;

    private void Awake()
    {
        _enemy    = GetComponent<Enemy>();
        _scroller = GetComponent<WorldScroller>();

        // Эффекты создаются лениво — при первом наложении статуса (см. UpdateStatusEffects).
        // Враг, которого не задело стихией, не плодит лишние объекты.
    }

    private void OnDisable()
    {
        // Сбрасываем все статусы при возврате в пул
        _expiryTime.Clear();
        if (_scroller != null) _scroller.SpeedMultiplier = 1f;

        SetEffectActive(ref _burningEffect, _burningPrefab, false);
        SetEffectActive(ref _frozenEffect,  _frozenPrefab,  false);
        SetEffectActive(ref _shockedEffect, _shockedPrefab, false);
    }

    /// <summary>Есть ли указанный статус на враге?</summary>
    public bool HasStatus(StatusEffectType status)
    {
        if (!_expiryTime.ContainsKey(status)) return false;
        return Time.time < _expiryTime[status];
    }

    /// <summary>
    /// Накладывает или обновляет статус.
    /// </summary>
    /// <param name="status">Какой статус наложить</param>
    /// <param name="hitDamage">Урон удара который наложил статус (нужен для Burning)</param>
    public void ApplyStatus(StatusEffectType status, int hitDamage)
    {
        if (status == StatusEffectType.None) return;

        bool isNew = !HasStatus(status);
        _expiryTime[status] = Time.time + DamageCalculator.STATUS_DURATION;

        switch (status)
        {
            case StatusEffectType.Burning:
                _burnDamagePerTick = Mathf.Max(1,
                    Mathf.RoundToInt(hitDamage * DamageCalculator.BURN_DAMAGE_PERCENT));
                _nextBurnTickTime = Time.time + (1f / DamageCalculator.BURN_TICKS_PER_SECOND);
                if (isNew) Debug.Log($"[Status] {gameObject.name} ПОДОЖЖЁН ({_burnDamagePerTick}/тик)", this);
                break;

            case StatusEffectType.Frozen:
                if (_scroller != null)
                {
                    _scroller.SpeedMultiplier = DamageCalculator.FROZEN_SPEED_MULTIPLIER;
                }
                else
                {
                    Debug.LogError($"[Status] {gameObject.name} НЕТ WorldScroller! Заморозка не работает.", this);
                }
                if (isNew) Debug.Log($"[Status] {gameObject.name} ЗАМОРОЖЕН (×{DamageCalculator.FROZEN_SPEED_MULTIPLIER})", this);
                break;

            case StatusEffectType.Shocked:
                if (isNew) Debug.Log($"[Status] {gameObject.name} ШОКИРОВАН (+{(DamageCalculator.SHOCK_DAMAGE_MULTIPLIER - 1f) * 100:F0}% урона)", this);
                break;
        }

        UpdateStatusEffects();
    }

    private void Update()
    {
        if (_enemy == null || _expiryTime.Count == 0) return;

        // ─── Burning — тикаем уроном ────────────────────────────
        if (HasStatus(StatusEffectType.Burning) && Time.time >= _nextBurnTickTime)
        {
            _enemy.TakeDamage(_burnDamagePerTick);
            _nextBurnTickTime = Time.time + (1f / DamageCalculator.BURN_TICKS_PER_SECOND);
        }

        // ─── Очищаем истёкшие статусы ───────────────────────────
        List<StatusEffectType> toRemove = null;
        foreach (var kv in _expiryTime)
        {
            if (Time.time >= kv.Value)
            {
                toRemove ??= new List<StatusEffectType>();
                toRemove.Add(kv.Key);
            }
        }

        if (toRemove != null)
        {
            foreach (var status in toRemove)
                RemoveStatus(status);
        }
    }

    private void RemoveStatus(StatusEffectType status)
    {
        _expiryTime.Remove(status);

        switch (status)
        {
            case StatusEffectType.Frozen:
                if (_scroller != null) _scroller.SpeedMultiplier = 1f;
                break;

            case StatusEffectType.Burning:
                break;

            case StatusEffectType.Shocked:
                break;
        }

        UpdateStatusEffects();
    }

    /// <summary>
    /// Включает/выключает эффекты статусов по их активности.
    /// Эффект виден ровно пока статус активен.
    /// </summary>
    private void UpdateStatusEffects()
    {
        SetEffectActive(ref _burningEffect, _burningPrefab, HasStatus(StatusEffectType.Burning));
        SetEffectActive(ref _frozenEffect,  _frozenPrefab,  HasStatus(StatusEffectType.Frozen));
        SetEffectActive(ref _shockedEffect, _shockedPrefab, HasStatus(StatusEffectType.Shocked));
    }

    /// <summary>
    /// Вкл/выкл эффект. Создаёт экземпляр из префаба ЛЕНИВО — только при первом включении.
    /// Дальше переиспользует тот же объект.
    /// </summary>
    private void SetEffectActive(ref GameObject instance, GameObject prefab, bool active)
    {
        // Нечего включать — и экземпляра ещё нет: не создаём зря.
        if (!active && instance == null) return;
        if (prefab == null) return;

        // Создаём при первом включении.
        if (instance == null)
        {
            instance = Instantiate(prefab, transform);
            instance.transform.localPosition = prefab.transform.localPosition;
        }

        if (instance.activeSelf != active)
        {
            instance.SetActive(active);
            if (active) PlayDesynced(instance);
        }
    }

    /// <summary>
    /// Запускает партиклы эффекта со случайного кадра,
    /// чтобы у толпы эффекты не были синхронными.
    /// </summary>
    private void PlayDesynced(GameObject effect)
    {
        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Clear(true);
            float randomOffset = Random.Range(0f, ps.main.duration);
            ps.Simulate(randomOffset, true, true);
            ps.Play(true);
        }
    }
}
