using UnityEngine;

/// <summary>
/// Состояние атаки ассасина — одна анимация с 3 взмахами.
/// Тайминг удара задаёт АНИМАЦИЯ (через OnSlash1/2/3 → NotifySlashDone).
/// Состояние только рвётся к цели и меняет её между взмахами.
/// </summary>
public class AssassinStrikeState : IUnitState
{
    private readonly MeleeUnitController _ctrl;
    private readonly AssassinAutoAttack  _assassinAttack;

    private Enemy _target;
    private int   _slashesDone;      // сколько взмахов уже случилось
    private bool  _needNewTarget;    // взмах прошёл — пора искать новую цель

    /// <summary>Текущая цель — её бьёт Animation Event.</summary>
    public Enemy CurrentTarget => _target;

    public AssassinStrikeState(MeleeUnitController ctrl, AssassinAutoAttack attack)
    {
        _ctrl           = ctrl;
        _assassinAttack = attack;
    }

    public void SetTarget(Enemy target) => _target = target;

    /// <summary>Вызывается из AssassinAutoAttack когда взмах нанёс удар.</summary>
    public void NotifySlashDone()
    {
        _slashesDone++;
        _needNewTarget = true;
        Debug.Log($"[AssassinStrike] Взмах {_slashesDone} нанесён", _ctrl);
    }

    public void Enter()
    {
        _slashesDone   = 0;
        _needNewTarget = false;

        // Запускаем анимацию НАПРЯМУЮ, без триггера (триггеры в проекте ненадёжны)
        _ctrl.PlayAttackRun();

        AnimatorStateInfo st = _ctrl.GetAnimatorState();
        Debug.Log($"[AssassinStrike] Серия началась. Анимация={(st.IsName("AttackRun") ? "AttackRun" : "Run/другое")}", _ctrl);
    }

    public void Exit()
    {
        if (_target != null)
        {
            _ctrl.ReleaseTarget(_target);
            _target = null;
        }
    }

    public void Tick()
    {
        // ── Конец серии: анимация доиграла ──
        AnimatorStateInfo state = _ctrl.GetAnimatorState();
        bool attackPlaying = state.IsName("AttackRun");
        bool attackDone    = attackPlaying && state.normalizedTime >= 0.95f;

        if (attackDone)
        {
            EndSeries();
            return;
        }

        // ── После взмаха — ищем новую цель для следующего ──
        if (_needNewTarget)
        {
            _needNewTarget = false;
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = FindNextTarget();
        }

        // ── Если цель умерла — ищем замену ──
        if (_target == null || !_target.gameObject.activeSelf)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = FindNextTarget();
        }

        // ── Рывок к текущей цели (пока анимация играет) ──
        if (_target != null)
        {
            Vector3 toEnemy  = _target.transform.position - _ctrl.transform.position;
            float   distance = toEnemy.magnitude;

            // Поворот к цели
            Vector3 lookDir = new Vector3(toEnemy.x, 0f, toEnemy.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                _ctrl.transform.rotation = Quaternion.Slerp(
                    _ctrl.transform.rotation, targetRot, 12f * Time.deltaTime);
            }

            // Движение пока не вплотную
            if (distance > _ctrl.AttackRange)
                _ctrl.transform.position += toEnemy.normalized * _ctrl.ChaseSpeed * Time.deltaTime;
        }
    }

    private void EndSeries()
    {
        _ctrl.transform.rotation = Quaternion.identity;
        _assassinAttack.StartSeriesCooldown();
        _ctrl.StartRejoin();
        _ctrl.ChangeState(_ctrl.FollowState);
    }

    private Enemy FindNextTarget()
    {
        Enemy next = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange);
        if (next != null) _ctrl.ClaimTarget(next);
        return next;
    }
}
