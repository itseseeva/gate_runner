using UnityEngine;

/// <summary>
/// Компонент автоатаки для range-юнитов (Mage, Archer, Tank).
/// Каждый кадр ищет ближайшего врага и атакует через IUnitAttack.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AutoAttacker : MonoBehaviour
{
    private IUnitAttack _attack;

    private void Awake()
    {
        // Ищем компонент который реализует IUnitAttack
        // (RangedAutoAttack, WarriorAutoAttack и т.д.)
        foreach (var comp in GetComponents<MonoBehaviour>())
        {
            if (comp is IUnitAttack attack)
            {
                _attack = attack;
                break;
            }
        }

        if (_attack == null)
            Debug.LogError($"[AutoAttacker] {gameObject.name}: нет компонента IUnitAttack!", this);
    }

    private void Update()
    {
        if (_attack == null) return;

        Enemy target = FindNearestEnemy(_attack.Range);
        if (target != null && _attack.IsReady)
            _attack.Hit(target);
    }

    private Enemy FindNearestEnemy(float range)
    {
        // TODO: заменить на кэшированный список (День 7)
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearest = null;
        float minDist = range;

        foreach (Enemy e in all)
        {
            if (!e.gameObject.activeSelf) continue;
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
