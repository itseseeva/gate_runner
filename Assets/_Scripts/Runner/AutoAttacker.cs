using UnityEngine;
using System.Collections.Generic;
 
/// <summary>
/// Компонент автоатаки. Использует явную ссылку на IUnitAttack.
/// Для танков: один танк — один враг (липкий claim).
/// Танк держит свою цель пока она жива и в радиусе, не перескакивает на ближайшую.
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
 
    // Какого врага держит какой танк. 1 танк на врага.
    private static readonly Dictionary<Enemy, AutoAttacker> _tankClaims = new();
 
    // Враг, которого держит ЭТОТ танк.
    private Enemy _myTarget;
 
    private float _lastHitLogTime = -999f;
 
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
 
        Enemy target;
 
        if (IsTank)
        {
            // Держим свою цель, пока она жива и в радиусе — НЕ перескакиваем.
            if (_myTarget != null && _myTarget.gameObject.activeSelf && InRange(_myTarget))
            {
                target = _myTarget;
            }
            else
            {
                // Своя цель умерла/ушла — освобождаем, ищем новую свободную.
                ReleaseMyClaim();
                target = FindNearestEnemy(_attack.Range);
                if (target == null) return;
                if (!TryClaim(target)) return; // занята другим танком — не бьём
            }
        }
        else
        {
            target = FindNearestEnemy(_attack.Range);
        }
 
        if (target == null || !_attack.IsReady) return;

        if (IsTank)
        {
            bool iOwn = _tankClaims.TryGetValue(target, out var h) && h == this;
            if (!iOwn) return;
 
            _lastHitLogTime = Time.time;
        }

        _attack.Hit(target);
    }
 
    /// <summary>Закрепляет врага за этим танком. true — можно бить.</summary>
    private bool TryClaim(Enemy target)
    {
        if (target == null) return false;
 
        // Уже мой — можно.
        if (_myTarget == target) return true;
 
        // Враг занят другим живым танком — нельзя.
        if (_tankClaims.TryGetValue(target, out AutoAttacker holder)
            && holder != null && holder != this && holder.gameObject.activeSelf)
            return false;
 
        // Свободен (или держал мёртвый танк) — забираем.
        ReleaseMyClaim();
        _tankClaims[target] = this;
        _myTarget = target;
        return true;
    }
 
    private void ReleaseMyClaim()
    {
        if (_myTarget != null
            && _tankClaims.TryGetValue(_myTarget, out AutoAttacker holder)
            && holder == this)
            _tankClaims.Remove(_myTarget);
        _myTarget = null;
    }
 
    private void OnDisable() => ReleaseMyClaim();
 
    private bool InRange(Enemy e)
    {
        if (e == null) return false;
        return Vector3.Distance(transform.position, e.transform.position) <= _attack.Range;
    }
 
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
            if (e.transform.position.z < myZ - _backCutoff) continue;
 
            // Танк не берёт цель, которую уже держит другой живой танк.
            if (IsTank
                && _tankClaims.TryGetValue(e, out AutoAttacker holder)
                && holder != null && holder != this && holder.gameObject.activeSelf)
                continue;
 
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