using UnityEngine;

/// <summary>
/// Летящий снаряд. Движется вперёд по Z на заданной скорости.
/// При попадании во врага наносит урон и возвращается в пул.
/// Если пролетел максимальную дистанцию — тоже возвращается.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Параметры")]
    [SerializeField] private float _speed       = 15f;
    [SerializeField] private float _maxDistance = 16f;

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

        // Raycast вперёд на шаг движения — гарантированное обнаружение
        if (Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, step))
        {
            Debug.Log($"[Projectile] Raycast попал в {hit.collider.name}");
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                ReturnToPool();
                return;
            }
        }

        transform.position += Vector3.forward * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _maxDistance)
            ReturnToPool();
    }



    private void ReturnToPool()
    {
        _active = false;
        ProjectilePool.Instance.Return(this);
    }
}
