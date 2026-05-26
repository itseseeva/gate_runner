using UnityEngine;
using System.Collections;

/// <summary>
/// Удар щитом — АОЕ отталкивание всех врагов в радиусе.
/// Независим от обычной атаки меча. Только для танка.
/// TODO: балансировать через Remote Config когда подключим LiveOps
/// </summary>
public class ShieldBash : MonoBehaviour
{
    [Header("Настройки удара щитом")]
    [Tooltip("Радиус АОЕ в метрах")]
    [SerializeField] private float _bashRadius = 3f;

    [Tooltip("Кулдаун между ударами щитом (секунды)")]
    [SerializeField] private float _cooldown = 3f;

    [Tooltip("Сила отталкивания от 0 до 1")]
    [Range(0f, 1f)]
    [SerializeField] private float _knockbackForce = 0.4f;

    [Tooltip("Слой на котором находятся враги")]
    [SerializeField] private LayerMask _enemyLayer;

    private float _lastBashTime = -999f;

    private void Update()
    {
        if (Time.time - _lastBashTime >= _cooldown)
        {
            TryBash();
            _lastBashTime = Time.time;
        }
    }

    private void TryBash()
    {
        // Ищем всех врагов в радиусе через Physics
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _bashRadius,
            _enemyLayer
        );

        if (hits.Length == 0) return;

        Debug.Log($"[ShieldBash] Удар щитом! Врагов в радиусе: {hits.Length}", this);

        foreach (Collider hit in hits)
        {
            KnockbackReceiver knockback = hit.GetComponent<KnockbackReceiver>();
            if (knockback == null) continue;

            // Направление отлёта — от танка к врагу
            Vector3 direction = (hit.transform.position - transform.position).normalized;
            // Добавляем немного вперёд по Z чтобы враги летели вперёд
            direction = (direction + Vector3.forward).normalized;

            Enemy enemy = hit.GetComponent<Enemy>();
            bool killed = false;
            // Щит не убивает — только отталкивает (урон 0)
            // TODO: добавить небольшой урон если нужно для баланса

            knockback.ApplyKnockback(direction, _knockbackForce, killed);
        }
    }

    /// <summary>Визуализация радиуса в редакторе — видно в Scene View.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _bashRadius);
    }
}
