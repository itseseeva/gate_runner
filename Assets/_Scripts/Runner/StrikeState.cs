using UnityEngine;

/// <summary>
/// Состояние "рывок и удар". Воин бежит к цели, бьёт один раз,
/// ищет следующую цель ВПЕРЕДИ по Z. Если нет — возврат в Follow.
/// </summary>
public class StrikeState : IUnitState
{
    private readonly MeleeUnitController _ctrl;
    private Enemy _target;
    private Enemy _lastHitEnemy;

    private bool _waitingAfterHit = false;

    public float LastHitZ { get; private set; }

    public StrikeState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    public void SetTarget(Enemy target)
    {
        _target = target;
    }

    public void Enter()
    {
        _waitingAfterHit = false;
        _ctrl.PlayAttackRun();

        // Отключаем AutoAttacker для всех (в т.ч. воинов), чтобы он не сжигал кулдаун во время рывка
        _ctrl.DisableAutoAttacker();
    }

    public void Exit()
    {
        _waitingAfterHit = false;
        if (_target != null)
        {
            _ctrl.ReleaseTarget(_target);
            _target = null;
        }

        // Возвращаем AutoAttacker при выходе из состояния
        _ctrl.EnableAutoAttacker();
    }

    /// <summary>
    /// Вызывается из Animation Event через контроллер.
    /// Проверяет цель и дистанцию, наносит удар если условия выполнены.
    /// </summary>
    public void OnAnimationHit()
    {
        if (_target == null || !_target.gameObject.activeSelf) return;

        var assassin = _ctrl.GetComponent<AssassinAutoAttack>();
        if (assassin == null) return;

        float distance = Vector3.Distance(_ctrl.transform.position, _target.transform.position);
        if (distance > _ctrl.AttackRange) return;

        assassin.DoSingleSlash(_target);
    }

    public void Tick()
    {
        // Ждём после удара чтобы анимация доиграла
        if (_waitingAfterHit)
        {
            // Ждём пока анимация AttackRun доиграет
            AnimatorStateInfo state = _ctrl.GetAnimatorState();
            bool attackDone = state.IsName("AttackRun") && state.normalizedTime >= 0.9f;
            bool notAttack = !state.IsName("AttackRun");
            
            if (attackDone || notAttack)
            {
                Debug.Log($"[STRIKE_END] {_ctrl.name} finishes wait. attackDone={attackDone} notAttack={notAttack} animState={state.shortNameHash}");
                _waitingAfterHit = false;
                FindNextOrReturn();
            }
            return;
        }

        // Цель умерла во время рывка
        if (_target == null || !_target.gameObject.activeSelf)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;

            // Танк: сразу домой, не ищем следующую цель
            if (_ctrl.IsTankUnit)
            {
                _ctrl.StartRejoin();
                _ctrl.ChangeState(_ctrl.FollowState);
                return;
            }

            _waitingAfterHit = true;
            return;
        }

        // Бежим к цели — только в плоскости XZ, игнорируем разницу по Y
        Vector3 toEnemy  = _target.transform.position - _ctrl.transform.position;
        toEnemy.y = 0f;
        float   distance = toEnemy.magnitude;

        if (distance <= _ctrl.AttackRange)
        {
            if (_ctrl.IsTankUnit)
            {
                {}
                if (_ctrl.AutoAttack == null || !_ctrl.AutoAttack.IsReady) return;

                _ctrl.AutoAttack.Hit(_target);

                LastHitZ = _target.transform.position.z;
                _lastHitEnemy = _target;
                _ctrl.ReleaseTarget(_target);
                _target = null;

                _ctrl.StartRejoin();
                _ctrl.ChangeState(_ctrl.FollowState);
                return;
            }

            if (_ctrl.AutoAttack == null || !_ctrl.AutoAttack.IsReady) return;

            DiagLogger.RecordHit(_ctrl.gameObject.GetInstanceID(), _target.GetInstanceID());

            _ctrl.AutoAttack.Hit(_target);

            LastHitZ = _target.transform.position.z;
            _lastHitEnemy = _target;

            _waitingAfterHit = true;
            return;
        }

        // Поводок: убежали от места в строю слишком далеко — бросаем погоню
        Vector3 homePos = _ctrl.Leader.position + _ctrl.FormationOffset;
        if (Vector3.Distance(_ctrl.transform.position, homePos) > _ctrl.MaxChaseDistance)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;
            _ctrl.StartRejoin();
            _ctrl.ChangeState(_ctrl.FollowState);
            return;
        }

        // Двигаемся к врагу только по XZ
        Vector3 dir = toEnemy.normalized;
        Vector3 newPos = _ctrl.transform.position + dir * _ctrl.ChaseSpeed * Time.deltaTime;
        newPos.y = _ctrl.transform.position.y; // Y не меняем!
        _ctrl.transform.position = newPos;
    }

    private void FindNextOrReturn()
    {
        // Все милишники — один удар за вылазку, затем сразу домой.
        // Чистый rejoin как у ассасина, без поиска следующей цели и беготни по толпе.
        _ctrl.StartRejoin();
        _ctrl.ChangeState(_ctrl.FollowState);
    }
}
