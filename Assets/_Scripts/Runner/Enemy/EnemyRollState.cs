using UnityEngine;

/// <summary>
/// Рывок-камикадзе. Враг летит вперёд по Z в отряд. При столкновении с героем —
/// AoE-урон вокруг точки удара, взрыв-эффект, затем смерть. Из этого состояния
/// выхода назад нет: только взрыв.
/// </summary>
public class EnemyRollState : EnemyState
{
    private static readonly Collider[] _hitBuffer = new Collider[16];

    private bool  _exploded;
    private bool  _launched;
    private float _enteredAt;
    private float _nextPushTime;

    public EnemyRollState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        // Скроллер НЕ выключаем — в фазе разгона враг едет с конвейером,
        // как до рывка. Выключим при переходе в полёт (в Tick).
        Ctrl.TriggerRoll();
        _exploded = false;
        _enteredAt = Time.time;
        _nextPushTime = 0f;
        _launched = false;
    }

    public override void Tick()
    {
        if (_exploded) return;
        if (Ctrl.Data == null) return;

        // ── Фаза разгона: едем с конвейером (как до рывка), не рвёмся, не толкаем. ──
        float startup = Ctrl.Data.RollStartupTime;
        if (Time.time < _enteredAt + startup)
        {
            // Скроллер сам двигает врага по -Z со скоростью мира — ничего не делаем.
            return;
        }

        // ── Переход в полёт (один раз): выключаем конвейер, дальше Z под нами. ──
        if (!_launched)
        {
            _launched = true;
            Ctrl.SetScroller(false);
        }

        // Летим вперёд к отряду. У гоблина forward смотрит ОТ цели (модель -190°),
        // поэтому двигаемся в сторону цели напрямую по вектору, а не по forward.
        Vector3 dir;
        if (Ctrl.Target != null)
        {
            dir = Ctrl.Target.transform.position - Ctrl.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            else dir = Vector3.forward;
        }
        else
        {
            dir = Vector3.forward;   // цель пропала — летим вперёд по инерции
        }

        Ctrl.transform.position += dir * Ctrl.Data.RollSpeed * Time.deltaTime;

        // Таран: тяжёлый роллер расталкивает ближних врагов в стороны, пока катится.
        // Толкаем соседей (не себя) — роллер прёт по прямой, толпа расступается.
        PushEnemiesAside();

        // Столкновение — узкая проверка по коллайдеру врага, а не по AoE-радиусу.
        // AoE-радиус это ЗОНА УРОНА при взрыве, а не дистанция срабатывания.
        float touchRadius = Ctrl.CombatColliderRadius + 0.1f;   // радиус врага + чуть-чуть
        int count = Physics.OverlapSphereNonAlloc(
            Ctrl.transform.position, touchRadius, _hitBuffer, Ctrl.HeroLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            IDamageable hero = _hitBuffer[i].GetComponentInParent<IDamageable>();
            if (hero != null && !hero.IsDead)
            {
                Explode();
                return;
            }
        }
    }

    /// <summary>Взрыв: AoE-урон по всем, эффект, триггер аниматора, смерть.</summary>
    private void Explode()
    {
        _exploded = true;

        var data = Ctrl.Data;
        Vector3 center = Ctrl.transform.position;

        // AoE-урон по всем героям в радиусе.
        int count = Physics.OverlapSphereNonAlloc(
            center, data.RollAoeRadius, _hitBuffer, Ctrl.HeroLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            IDamageable hero = _hitBuffer[i].GetComponentInParent<IDamageable>();
            if (hero != null && !hero.IsDead)
                hero.TakeDamage(data.RollDamage, true, DamageNumberType.Normal);
        }

        // Взрыв-эффект — сюда добавишь свою визуальную крутость.
        if (data.RollExplosionEffect != null && VfxPool.Instance != null)
            VfxPool.Instance.Spawn(center, Quaternion.identity, data.RollExplosionEffect);

        Ctrl.TriggerRollEnd();   // аниматор: Roll_Loop → выход

        // Камикадзе — враг гибнет от собственного рывка.
        Ctrl.KillSelf();
    }

    private static readonly Collider[] _pushBuffer = new Collider[16];

    /// <summary>Толкает врагов, которых реально накрыл габарит роллера (по касанию).</summary>
    private void PushEnemiesAside()
    {
        if (Time.time < _nextPushTime) return;
        _nextPushTime = Time.time + Ctrl.Data.RollPushInterval;

        Vector3 myPos = Ctrl.transform.position;
        float touchRadius = Ctrl.CombatColliderRadius + Ctrl.Data.RollPushMargin;

        int count = Physics.OverlapSphereNonAlloc(
            myPos, touchRadius, _pushBuffer, Ctrl.EnemyLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            EnemyCombatBase other = _pushBuffer[i].GetComponentInParent<EnemyCombatBase>();
            if (other == null || other == Ctrl) continue;

            Vector3 d = other.transform.position - myPos;
            d.y = 0f;
            float dSqr = d.x * d.x + d.z * d.z;
            if (dSqr < 0.0001f) continue;

            other.ApplyKnockback(d, Ctrl.Data.RollPushForce);
        }
    }
}
