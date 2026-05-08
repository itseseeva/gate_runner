using UnityEngine;

/// <summary>
/// Летящий снаряд. Движется вперёд по Z на заданной скорости.
/// При попадании во врага наносит урон и возвращается в пул.
/// Несёт стихию от стрелка — она применяется при попадании.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Параметры")]
    [SerializeField] private float _speed       = 15f;
    [SerializeField] private float _maxDistance = 16f;

    private int         _damage;
    private float       _distanceTravelled;
    private bool        _active;
    private ElementType _element = ElementType.None;

    /// <summary>Запускает снаряд с указанным уроном, дистанцией и стихией.</summary>
    public void Launch(int damage, float maxDistance, ElementType element)
    {
        _damage            = damage;
        _maxDistance       = maxDistance;
        _element           = element;
        _distanceTravelled = 0f;
        _active            = true;

        UpdateVisualForElement();
    }

    private void UpdateVisualForElement()
    {
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        Color color = _element switch
        {
            ElementType.Fire      => new Color(1f, 0.4f, 0.1f),
            ElementType.Ice       => new Color(0.4f, 0.8f, 1f),
            ElementType.Lightning => new Color(1f, 0.95f, 0.3f),
            _                     => Color.white,
        };

        Material mat = renderer.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }

    private void Update()
    {
        if (!_active) return;

        float step = _speed * Time.deltaTime;
        transform.position += Vector3.forward * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _maxDistance)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        // Применяем урон через DamageCalculator (учитывает стихию и Shocked-статус)
        StatusController status = enemy.GetComponent<StatusController>();
        int finalDamage = DamageCalculator.CalculateFinalDamage(_damage, _element, status);

        bool died = enemy.TakeDamage(finalDamage);

        // Если враг жив и атакующий со стихией — накладываем статус
        if (!died && _element != ElementType.None && status != null)
        {
            StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(_element);
            status.ApplyStatus(statusToApply, finalDamage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _active = false;
        ProjectilePool.Instance.Return(this);
    }
}
