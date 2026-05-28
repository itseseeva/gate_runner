using UnityEngine;

/// <summary>
/// Компонент автоатаки. Использует явную ссылку на IUnitAttack
/// вместо слепого поиска — чтобы не подхватить не тот компонент.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AutoAttacker : MonoBehaviour
{
    [Header("Атака")]
    [Tooltip("Перетащи сюда WarriorAutoAttack / RangedAutoAttack")]
    [SerializeField] private MonoBehaviour _attackComponent;

    private IUnitAttack _attack;

    private void Awake()
    {
        _attack = _attackComponent as IUnitAttack;

        if (_attack == null)
            Debug.LogError($"[AutoAttacker] {gameObject.name}: _attackComponent не реализует IUnitAttack!", this);
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
