using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул снарядов. Никаких Instantiate в бою.
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int        _preloadCount = 30;

    private readonly Queue<Projectile> _pool = new();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < _preloadCount; i++)
        {
            Projectile p = CreateProjectile();
            p.gameObject.SetActive(false);
            _pool.Enqueue(p);
        }
    }

    private Projectile CreateProjectile()
    {
        GameObject go = Instantiate(_projectilePrefab);
        return go.GetComponent<Projectile>();
    }

    /// <summary>Берёт снаряд из пула и ставит в позицию стрелка.</summary>
    public Projectile Get(Vector3 position, Quaternion rotation)
    {
        Projectile p = _pool.Count > 0
            ? _pool.Dequeue()
            : CreateProjectile();

        p.transform.position = position;
        p.transform.rotation = rotation;
        p.gameObject.SetActive(true);
        return p;
    }

    /// <summary>Возвращает снаряд в пул.</summary>
    public void Return(Projectile p)
    {
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }
}
