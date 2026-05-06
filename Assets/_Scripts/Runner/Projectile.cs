using UnityEngine;

/// <summary>
/// Летящий снаряд. Движется вперёд по Z на заданной скорости.
/// При попадании во врага наносит урон и возвращается в пул.
/// Если пролетел максимальную дистанцию — тоже возвращается.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Параметры")]
    [SerializeField] private float _speed        = 15f;
    [SerializeField] private float _maxDistance  = 16f;
    [SerializeField] private float _hitRadius    = 0.3f; // размер хитбокса

    private int   _damage;
    private float _distanceTravelled;
    private bool  _active;

    /// <summary>Запускает снаряд с указанным уроном и максимальной дистанцией.</summary>
    public void Launch(int damage, float maxDistance)
    {
        _damage            = damage;
        _maxDistance       = maxDistance;
        _distanceTravelled = 0f;
        _active            = true;
    }

    private void Update()
    {
        if (!_active) return;

        float step = _speed * Time.deltaTime;
        const float HitDetectionPadding = 0.5f;
        float rayLength = step + HitDetectionPadding;

        // SphereCast вместо Raycast — шар летит вперёд, не промахивается мимо врага
        if (Physics.SphereCast(transform.position, _hitRadius, transform.forward, out RaycastHit hit, rayLength))
        {
            Debug.Log($"[Projectile] SphereCast попал в {hit.collider.name}", this);
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                ReturnToPool();
                return;
            }
        }

        transform.position += transform.forward * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _maxDistance)
            ReturnToPool();
    }

    private void OnDrawGizmos()
    {
        if (!_active) return;
        Gizmos.color = Color.red;
        // Рисуем сферу — это наш хитбокс снаряда
        Gizmos.DrawWireSphere(transform.position + transform.forward * _hitRadius, _hitRadius);
    }



    private void ReturnToPool()
    {
        _active = false;
        ProjectilePool.Instance.Return(this);
    }
}
