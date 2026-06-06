using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент автоатаки. Использует явную ссылку на IUnitAttack.
/// Для танков добавлена проверка резерва цели: один танк — один враг.
/// Лишние танки (которым не досталось свободной цели) не атакуют пустоту.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AutoAttacker : MonoBehaviour
{
    [Header("Атака")]
    [Tooltip("Перетащи сюда WarriorAutoAttack / RangedAutoAttack")]
    [SerializeField] private MonoBehaviour _attackComponent;

    [Tooltip("Насколько далеко назад танк ещё видит врага. Меньше = не гонится за отлетевшими.")]
    [SerializeField] private float _backCutoff = 0.5f;

    private IUnitAttack _attack;
    private Unit _unit;

    // Какого врага держит какой танк (общий реестр для всех танков).
    // 1 танк на врага.
    private static readonly Dictionary<Enemy, AutoAttacker> _tankClaims = new();

    // Враг, которого держит ЭТОТ танк прямо сейчас.
    private Enemy _myTarget;

    private void Awake()
    {
        _attack = _attackComponent as IUnitAttack;
        _unit   = GetComponent<Unit>();

        if (_attack == null)
            Debug.LogError($"[AutoAttacker] {gameObject.name}: _attackComponent не реализует IUnitAttack!", this);
    }

    private bool IsTank => _unit != null && _unit.HeroType == HeroType.Tank;

    private void Update()
    {
        if (_attack == null) return;

        Enemy target = FindNearestEnemy(_attack.Range);

        // Танк: проверяем резерв цели
        if (IsTank)
        {
            // Цель пропала или сменилась — освобождаем старую
            if (_myTarget != null && (target != _myTarget || !_myTarget.gameObject.activeSelf))
                ReleaseMyClaim();

            if (target == null) return;

            // Пытаемся застолбить цель. Не вышло (держит другой танк) — не бьём.
            if (!TryClaim(target)) return;
        }

        if (target != null && _attack.IsReady)
            _attack.Hit(target);
    }

    /// <summary>Пытается закрепить врага за этим танком. true — можно бить.</summary>
    private bool TryClaim(Enemy target)
    {
        // Уже мой — можно.
        if (_myTarget == target) return true;

        // Враг занят другим живым танком — нельзя.
        if (_tankClaims.TryGetValue(target, out AutoAttacker holder)
            && holder != null && holder != this)
            return false;

        // Свободен — забираем, освобождая прошлого.
        ReleaseMyClaim();
        _tankClaims[target] = this;
        _myTarget = target;
        return true;
    }

    private void ReleaseMyClaim()
    {
        if (_myTarget != null && _tankClaims.TryGetValue(_myTarget, out AutoAttacker holder)
            && holder == this)
            _tankClaims.Remove(_myTarget);
        _myTarget = null;
    }

    private void OnDisable() => ReleaseMyClaim();

    private Enemy FindNearestEnemy(float range)
    {
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearest = null;
        float minDist = range;

        float myZ = transform.position.z;

        foreach (Enemy e in all)
        {
            if (!e.gameObject.activeSelf) continue;

            // Не бьём врагов, которых откинуло назад (позади танка по Z).
            // _backCutoff — небольшой допуск, чтобы вровень стоящие считались.
            if (e.transform.position.z < myZ - _backCutoff) continue;

            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e;
            }
        }
        return nearest;
    }
}
