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
/// - Frozen меняет _speedMultiplier у WorldScroller врага.
/// - Shocked — флаг для DamageCalculator (никаких локальных эффектов).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class StatusController : MonoBehaviour
{
    private Enemy          _enemy;
    private WorldScroller  _scroller;
    private MeshRenderer   _renderer;
    private Color          _originalColor;
    private bool           _originalColorCached;

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
        _renderer = GetComponentInChildren<MeshRenderer>();

        // Кешируем оригинальный цвет один раз
        if (_renderer != null && !_originalColorCached)
        {
            Material mat = _renderer.material;
            _originalColor = mat.HasProperty("_BaseColor")
                ? mat.GetColor("_BaseColor")
                : mat.color;
            _originalColorCached = true;
        }
    }

    private void OnDisable()
    {
        // Сбрасываем все статусы при возврате в пул
        _expiryTime.Clear();
        if (_scroller != null) _scroller.SpeedMultiplier = 1f;
        RestoreOriginalColor();
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

        // Применяем эффекты конкретных статусов
        switch (status)
        {
            case StatusEffectType.Burning:
                // Урон тика = % от удара. Обновляется при каждом ударе.
                _burnDamagePerTick = Mathf.Max(1,
                    Mathf.RoundToInt(hitDamage * DamageCalculator.BURN_DAMAGE_PERCENT));
                _nextBurnTickTime = Time.time + (1f / DamageCalculator.BURN_TICKS_PER_SECOND);
                if (isNew) Debug.Log($"[Status] {gameObject.name} ПОДОЖЖЁН ({_burnDamagePerTick}/тик)", this);
                break;

            case StatusEffectType.Frozen:
                if (_scroller != null) 
                {
                    _scroller.SpeedMultiplier = DamageCalculator.FROZEN_SPEED_MULTIPLIER;
                    Debug.Log($"[Status] {gameObject.name} _scroller.SpeedMultiplier = {_scroller.SpeedMultiplier}", this);
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

        UpdateColorByStatus();
    }

    private void Update()
    {
        if (_enemy == null || _expiryTime.Count == 0) return;

        // ─── Burning — тикаем уроном ────────────────────────────
        if (HasStatus(StatusEffectType.Burning) && Time.time >= _nextBurnTickTime)
        {
            _enemy.TakeDamage(_burnDamagePerTick);
            _nextBurnTickTime = Time.time + (1f / DamageCalculator.BURN_TICKS_PER_SECOND);
            Debug.Log($"[Status] {gameObject.name} тик поджога: -{_burnDamagePerTick} HP", this);
        }

        // ─── Очищаем истёкшие статусы ───────────────────────────
        // Используем list чтобы не модифицировать словарь во время итерации
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
            {
                RemoveStatus(status);
            }
        }
    }

    private void RemoveStatus(StatusEffectType status)
    {
        _expiryTime.Remove(status);

        // Убираем эффект конкретного статуса
        switch (status)
        {
            case StatusEffectType.Frozen:
                if (_scroller != null) _scroller.SpeedMultiplier = 1f;
                Debug.Log($"[Status] {gameObject.name} разморожен", this);
                break;

            case StatusEffectType.Burning:
                Debug.Log($"[Status] {gameObject.name} перестал гореть", this);
                break;

            case StatusEffectType.Shocked:
                Debug.Log($"[Status] {gameObject.name} больше не шокирован", this);
                break;
        }

        UpdateColorByStatus();
    }

    /// <summary>
    /// Обновляет цвет врага в зависимости от активного статуса.
    /// Приоритет: Burning > Frozen > Shocked > Original.
    /// </summary>
    private void UpdateColorByStatus()
    {
        if (_renderer == null) return;

        Color targetColor;

        if (HasStatus(StatusEffectType.Burning))
            targetColor = new Color(1f, 0.4f, 0.1f);    // оранжевый
        else if (HasStatus(StatusEffectType.Frozen))
            targetColor = new Color(0.4f, 0.8f, 1f);    // голубой
        else if (HasStatus(StatusEffectType.Shocked))
            targetColor = new Color(1f, 0.95f, 0.3f);   // жёлтый
        else
            targetColor = _originalColor;

        Material mat = _renderer.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", targetColor);
        else
            mat.color = targetColor;
    }

    private void RestoreOriginalColor()
    {
        if (_renderer == null || !_originalColorCached) return;
        Material mat = _renderer.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", _originalColor);
        else
            mat.color = _originalColor;
    }
}
