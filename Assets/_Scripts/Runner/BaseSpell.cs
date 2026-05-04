using UnityEngine;

/// <summary>
/// Базовый класс способности. Дочерние классы реализуют Execute().
/// </summary>
public abstract class BaseSpell : MonoBehaviour
{
    [Header("Базовые настройки")]
    [SerializeField] protected float _cooldown   = 1f;
    [SerializeField] protected int   _damage     = 10;

    protected float _lastFireTime = -999f;

    public bool IsReady => Time.time - _lastFireTime >= _cooldown;

    /// <summary>Выполняет способность по цели.</summary>
    public void TryCast(Enemy target)
    {
        if (!IsReady || target == null) return;
        _lastFireTime = Time.time;
        Execute(target);
    }

    protected abstract void Execute(Enemy target);
}
