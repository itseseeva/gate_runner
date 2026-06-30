using UnityEngine;
using DG.Tweening;

/// <summary>
/// Коллизионная атака танка с двухфазной системой:
/// 1. Враг входит в зону _anticipationRange → анимация атаки стартует заранее.
/// 2. Враг касается триггер-коллайдера танка → мгновенный урон + knockback + VFX.
/// </summary>
[RequireComponent(typeof(WarriorAutoAttack))]
public class TankContactAttack : MonoBehaviour
{
    [Header("Кулдаун")]
    [Tooltip("Минимальный интервал между ударами (сек)")]
    [SerializeField] private float _contactCooldown = 4.0f;

    [Header("Захват и Антиципация")]
    [Tooltip("Радиус выбора цели — танк резервирует врага и начинает к нему поворачиваться")]
    [SerializeField] private float _targetSelectionRange = 4.0f;

    [Tooltip("Радиус запуска анимации — враг подошёл достаточно близко, чтобы начать замах")]
    [SerializeField] private float _animAnticipationRange = 1.5f;

    [Tooltip("Длительность анимации атаки — блокирует повторный триггер на это время")]
    [SerializeField] private float _animDuration = 0.7f;

    [Header("Поворот")]
    [Tooltip("Максимальный угол поворота к цели (градусы влево/вправо)")]
    [SerializeField] private float _maxRotationAngle = 35f;

    [Header("Рывок")]
    [Tooltip("Скорость рывка (м/с)")]
    [SerializeField] private float _dashSpeed = 15f;
    
    [Tooltip("Максимальная дистанция рывка")]
    [SerializeField] private float _maxDashDistance = 2.5f;

    [Tooltip("Дистанция при которой считается что танк коснулся врага")]
    [SerializeField] private float _hitDistance = 0.6f;

    private WarriorAutoAttack   _attack;
    private Animator            _animator;
    private MeleeUnitController _meleeCtrl;

    private float _lastContactTime     = -999f;
    private float _lastAnimTriggerTime = -999f; // когда последний раз поставили триггер анимации

    private bool _isDashing = false;
    private float _dashTimeLeft = 0f;

    private void Awake()
    {
        _attack   = GetComponent<WarriorAutoAttack>();
        _animator = GetComponentInChildren<Animator>();
        _meleeCtrl = GetComponent<MeleeUnitController>();

        // Ищем корень юнита, чтобы найти все дублирующиеся компоненты в иерархии
        Unit unit = GetComponentInParent<Unit>();
        Transform searchRoot = unit != null ? unit.transform : transform.root;

        TankContactAttack[] all = searchRoot.GetComponentsInChildren<TankContactAttack>(includeInactive: true);
        if (all.Length > 1 && all[0] != this)
        {
            Debug.LogWarning($"[TankContactAttack] Дубликат на '{gameObject.name}' — отключаем. Активен: '{all[0].gameObject.name}'", this);
            enabled = false;
            return;
        }

        if (_attack == null)
            Debug.LogError("[TankContactAttack] WarriorAutoAttack не найден!", this);
    }

    private Enemy _targetedEnemy;

    private Quaternion ClampRotation(Quaternion rot)
    {
        float angle = Quaternion.Angle(Quaternion.identity, rot);
        if (angle > _maxRotationAngle)
        {
            return Quaternion.Slerp(Quaternion.identity, rot, _maxRotationAngle / angle);
        }
        return rot;
    }

    private void Update()
    {
        // Плавный поворот к цели или возвращение к прямому направлению (Vector3.forward)
        if (_targetedEnemy != null && _targetedEnemy.gameObject.activeSelf)
        {
            Vector3 dir = (_targetedEnemy.transform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = ClampRotation(Quaternion.LookRotation(dir));
                // Slerp даёт более плавное вращение с естественным замедлением в конце
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
            }
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 10f * Time.deltaTime);
        }

        // Кулдаун между ударами
        if (Time.time - _lastContactTime < _contactCooldown) return;

        // Анимация была запущена недавно — ждём пока доиграет
        float timeSinceAnim = Time.time - _lastAnimTriggerTime;
        if (timeSinceAnim < _animDuration)
            return;

        // Возвращаем танк в строй (управление от SquadController), если он был отключён на время рывка
        if (_meleeCtrl != null && !_meleeCtrl.IsInFormation)
            _meleeCtrl.IsInFormation = true;

        // Если время вышло, а удар так и не случился — снимаем привязку
        if (_targetedEnemy != null)
        {
            if (_targetedEnemy.TargetedByTank == this)
                _targetedEnemy.TargetedByTank = null;
            _targetedEnemy = null;
        }

        // Ищем БЛИЖАЙШЕГО свободного врага в зоне раннего захвата
        Collider[] cols = Physics.OverlapSphere(transform.position, _targetSelectionRange);
        Enemy closestEnemy = null;
        float minDistance  = float.MaxValue;

        foreach (Collider col in cols)
        {
            if (!col.gameObject.activeSelf) continue;
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null) continue;

            // Если врага уже прицелил ДРУГОЙ танк — игнорируем
            if (enemy.TargetedByTank != null && enemy.TargetedByTank != this) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            // Резервируем врага за этим танком (уже на большой дистанции, чтобы начать поворачиваться)
            _targetedEnemy = closestEnemy;
            closestEnemy.TargetedByTank = this;

            // Но анимацию замаха запускаем только если он подошёл достаточно близко
            if (minDistance <= _animAnticipationRange && _animator != null)
            {
                _animator.SetTrigger("Attack");
                _lastAnimTriggerTime = Time.time; // блокируем повтор на _animDuration секунд

                // Отключаем влияние строя, чтобы мы могли сами двигать танк
                if (_meleeCtrl != null)
                    _meleeCtrl.IsInFormation = false;

                // Начинаем активный рывок-преследование
                _isDashing = true;
                _dashTimeLeft = _maxDashDistance / _dashSpeed; // максимальное время рывка, чтобы не зависнуть
            }
        }
    }

    private void LateUpdate()
    {
        // Активный самонаводящийся рывок
        if (_isDashing)
        {
            // Цель умерла во время рывка — пробуем найти соседнего врага
            if (_targetedEnemy == null || !_targetedEnemy.gameObject.activeSelf)
            {
                Enemy replacement = FindNearbyReplacement();
                if (replacement != null)
                {
                    _targetedEnemy = replacement;
                    replacement.TargetedByTank = this;
                }
                else
                {
                    _isDashing = false;
                    return;
                }
            }

            _dashTimeLeft -= Time.deltaTime;

            // Двигаемся к врагу
            transform.position = Vector3.MoveTowards(
                transform.position, _targetedEnemy.transform.position, _dashSpeed * Time.deltaTime);

            // Дистанционная проверка попадания — не ждём OnTriggerEnter
            float dist = Vector3.Distance(transform.position, _targetedEnemy.transform.position);
            if (dist <= _hitDistance)
            {
                TryHitDuringDash(_targetedEnemy);
                return;
            }

            // Время рывка вышло
            if (_dashTimeLeft <= 0)
                _isDashing = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.activeSelf) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        Debug.Log($"[Tank Hit Debug] {gameObject.name} коснулся {other.name}", this);

        // Если врага бьёт другой танк — мы его игнорируем
        if (enemy.TargetedByTank != null && enemy.TargetedByTank != this)
        {
            Debug.Log($"[Tank Hit Debug] Удар отменён: {enemy.name} уже занят танком {enemy.TargetedByTank.name}", this);
            return;
        }

        float timeSinceLast = Time.time - _lastContactTime;
        if (timeSinceLast < _contactCooldown)
        {
            Debug.Log($"[Tank Hit Debug] Удар отменён: кулдаун ещё не прошёл ({timeSinceLast:F2}s < {_contactCooldown}s)", this);
            return;
        }

        _lastContactTime = Time.time;

        // После успешного удара снимаем резерв, чтобы после отлёта врага мог ударить кто угодно
        if (enemy.TargetedByTank == this)
            enemy.TargetedByTank = null;
        if (_targetedEnemy == enemy)
            _targetedEnemy = null;

        Debug.Log($"[Tank Hit Debug] УСПЕШНЫЙ УДАР по {enemy.name}! Вызываем ApplyTankHit.", this);

        // Моментально доворачиваемся к врагу перед самым ударом (в пределах допустимого угла)
        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
        dirToEnemy.y = 0;
        if (dirToEnemy != Vector3.zero)
            transform.rotation = ClampRotation(Quaternion.LookRotation(dirToEnemy));

        // Мгновенный удар: урон + knockback + VFX
        // Анимация уже запущена из Update() антиципацией
        _attack.ApplyTankHit(enemy);

        // 1. Сразу же возвращаем танк в строй, не дожидаясь конца анимации
        if (_meleeCtrl != null && !_meleeCtrl.IsInFormation)
            _meleeCtrl.IsInFormation = true;

        // Останавливаем рывок
        _isDashing = false;
    }

    /// <summary>
    /// Ищет ближайшего живого врага в радиусе _animAnticipationRange.
    /// Используется когда основная цель умерла во время рывка.
    /// </summary>
    private Enemy FindNearbyReplacement()
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy best = null;
        float bestDist = float.MaxValue;

        foreach (Enemy e in all)
        {
            if (e == null || !e.gameObject.activeSelf) continue;
            if (e.TargetedByTank != null && e.TargetedByTank != this) continue;

            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d > _animAnticipationRange) continue;

            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        return best;
    }

    /// <summary>
    /// Применяет удар по врагу во время рывка (без OnTriggerEnter).
    /// Проверяет кулдаун и состояние врага.
    /// </summary>
    private void TryHitDuringDash(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeSelf) return;
        if (Time.time - _lastContactTime < _contactCooldown) return;

        _lastContactTime = Time.time;

        if (enemy.TargetedByTank == this)
            enemy.TargetedByTank = null;
        _targetedEnemy = null;

        Debug.Log($"[Tank Hit Debug] УДАР по {enemy.name} через дистанцию (dash hit).", this);

        // Доворот перед ударом
        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
        dirToEnemy.y = 0;
        if (dirToEnemy != Vector3.zero)
            transform.rotation = ClampRotation(Quaternion.LookRotation(dirToEnemy));

        _attack.ApplyTankHit(enemy);

        if (_meleeCtrl != null && !_meleeCtrl.IsInFormation)
            _meleeCtrl.IsInFormation = true;

        _isDashing = false;
    }
}
