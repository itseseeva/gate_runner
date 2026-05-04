using UnityEngine;

/// <summary>
/// Компонент автоатаки юнита.
/// Каждый кадр ищет ближайшего врага и бьёт если готово.
/// </summary>
public class AutoAttacker : MonoBehaviour
{
    [SerializeField] private float _attackRange = 5f;
    
    private BaseSpell _spell;

    private void Awake()
    {
        _spell = GetComponent<BaseSpell>();
    }

    private void Update()
    {
        if (_spell == null) return;
        
        Enemy target = FindNearestEnemy();
        if (target != null)
            _spell.TryCast(target);
    }

    private Enemy FindNearestEnemy()
    {
        // TODO: заменить на кэшированный список врагов для оптимизации
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        
        Enemy nearest  = null;
        float minDist  = _attackRange;

        foreach (Enemy e in allEnemies)
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
