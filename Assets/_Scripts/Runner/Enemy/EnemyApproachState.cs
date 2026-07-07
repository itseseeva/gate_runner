using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Стейт "враг движется к отряду". Едет через WorldScroller,
/// тянется по X к ближайшему юниту, отталкивается от других врагов.
/// Каждый кадр проверяет — есть ли цель в дистанции удара.
/// Если да → переход в EnemyAttackState.
/// </summary>
public class EnemyApproachState : EnemyStateBase
{
    // Параметры хаоса — TODO вынести в EnemyDefinitionSO при балансировке
    private const float TrackingRange   = 8f;
    private const float TrackingSpeed   = 3f;
    // SeparationRadius теперь берётся из _ctrl.SeparationRadius (SO)
    private const float SeparationForce  = 4f;
    private const float WobbleAmount    = 0.3f;
    private const float WobbleSpeed     = 2f;
    private const float LazyChance      = 0.3f;
    private const float LazyDuration    = 0.5f;
    private const float LazyCheckPeriod = 1f;

    // Статический список для быстрого поиска соседей
    private static readonly List<EnemyApproachState> _allApproaching = new();

    private SquadController _squad;
    private Unit  _target;
    private float _personalSpeedFactor;
    private float _wobblePhase;
    private float _nextLazyCheck;
    private float _lazyUntil;

    public EnemyApproachState(EnemyController ctrl) : base(ctrl) { }

    public override void Enter()
    {
        _allApproaching.Add(this);

        _personalSpeedFactor = Random.Range(0.7f, 1.3f);
        _wobblePhase         = Random.Range(0f, Mathf.PI * 2f);
        _nextLazyCheck       = 0f;
        _lazyUntil           = 0f;

        if (_squad == null)
            _squad = Object.FindAnyObjectByType<SquadController>();

        // Играем анимацию бега
        if (_ctrl.Animator != null)
            _ctrl.Animator.Play("Run");

        // WorldScroller двигает мир — включаем движение врага
        if (_ctrl.Scroller != null)
            _ctrl.Scroller.enabled = true;

        SelectAndClaimTarget();

        Debug.Log($"[EnemyApproach ENTER] {_ctrl.name}: pos={_ctrl.transform.position:F3}", _ctrl);
    }

    public override void Exit()
    {
        _allApproaching.Remove(this);

        // Обнуляем локальный кеш — регистрацию не трогаем,
        // она "переходит" к AttackState вместе с целью.
        _target = null;

        Debug.Log($"[EnemyApproach EXIT] {_ctrl.name}: pos={_ctrl.transform.position:F3}", _ctrl);
    }

    public override void Tick()
    {
        // 1. Если цель умерла или пропала — перевыбираем через реестр
        if (_target == null || _target.IsDead || !_target.gameObject.activeSelf)
            SelectAndClaimTarget();

        Unit target = _target;

        // 2. Проверка: цель в дистанции удара? → переход в атаку
        if (target != null)
        {
            float distSqr = SqrDistanceXZ(_ctrl.transform.position, target.transform.position);
            float rangeSqr = _ctrl.AttackRange * _ctrl.AttackRange;
            if (distSqr <= rangeSqr)
            {
                _ctrl.SwitchTo(new EnemyAttackState(_ctrl, target));
                return;
            }
        }

        // 3. Иначе — двигаемся как раньше (хаос + отталкивание)
        UpdateMovement(target);
    }

    /// <summary>
    /// Выбирает лучшую цель через реестр (наименее заклеймленную)
    /// и регистрирует своё намерение к ней идти.
    /// Если у нас уже была цель — сначала снимаем старую регистрацию.
    /// </summary>
    private Unit SelectAndClaimTarget()
    {
        if (_squad == null) return null;

        // Освобождаем старую цель если была
        if (_target != null)
            EnemyTargetRegistry.Unregister(_target);

        // Берём наименее заклеймленную из живых
        _target = EnemyTargetRegistry.GetLeastAttacked(_ctrl.transform.position, _squad);

        // Регистрируем что мы к ней идём
        if (_target != null)
            EnemyTargetRegistry.Register(_target);

        return _target;
    }

    private void UpdateMovement(Unit target)
    {
        // Lazy — иногда враг решает "лениться" секунду
        float speedMul = _personalSpeedFactor;
        if (Time.time >= _nextLazyCheck)
        {
            _nextLazyCheck = Time.time + LazyCheckPeriod;
            if (Random.value < LazyChance)
                _lazyUntil = Time.time + LazyDuration;
        }
        if (Time.time < _lazyUntil)
            speedMul *= 0.3f;

        // Wobble по X — каждый враг качается со своей фазой
        float wobble = Mathf.Sin(Time.time * WobbleSpeed + _wobblePhase)
                     * WobbleAmount * Time.deltaTime;

        // Tracking по X — тянемся к цели
        float trackingDeltaX = 0f;
        if (target != null)
        {
            Vector3 myPos = _ctrl.transform.position;
            Vector3 toTarget = target.transform.position - myPos;
            float distXZ = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            if (distXZ < TrackingRange && distXZ > 0.01f)
            {
                float dirX = toTarget.x / distXZ;
                trackingDeltaX = dirX * TrackingSpeed * speedMul * Time.deltaTime;
            }
        }

        // Отталкивание от других врагов
        float separationDeltaX = 0f;
        float separationDeltaZ = 0f;
        Vector3 myPos2 = _ctrl.transform.position;

        float sepRadius = _ctrl.SeparationRadius;

        foreach (EnemyApproachState other in _allApproaching)
        {
            if (other == this) continue;
            Vector3 d = myPos2 - other._ctrl.transform.position;
            float distSqr = d.x * d.x + d.z * d.z;
            float radSqr = sepRadius * sepRadius;
            if (distSqr > radSqr) continue;

            float dist = Mathf.Sqrt(distSqr);
            if (dist < 0.01f) dist = 0.01f;

            float strength = (1f - dist / sepRadius) * SeparationForce * speedMul * Time.deltaTime;
            separationDeltaX += (d.x / dist) * strength;
            separationDeltaZ += (d.z / dist) * strength;
        }

        // Применяем итоговое смещение
        Vector3 pos = _ctrl.transform.position;
        pos.x += trackingDeltaX + separationDeltaX + wobble;
        pos.z += separationDeltaZ;
        _ctrl.transform.position = pos;
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
