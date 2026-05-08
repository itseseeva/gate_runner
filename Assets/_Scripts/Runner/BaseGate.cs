using UnityEngine;
using TMPro;

/// <summary>
/// Базовый класс ворот. Дочерние классы реализуют ApplyEffect().
/// Срабатывает только когда лидер реально проходит МЕЖДУ стёкол по X.
/// </summary>
public abstract class BaseGate : MonoBehaviour
{
    [Header("Визуал")]
    [SerializeField] private TextMeshPro _label;

    [Header("Зона срабатывания (по X относительно ворот)")]
    [Tooltip("Полуширина прохода между стёкол по X. Стандартное расстояние стёкол = ±1.5, значит проход ~1.4")]
    [SerializeField] private float _passageHalfWidth = 1.4f;

    private bool _alreadyTriggered = false;

    protected virtual void Start()
    {
        if (_label != null)
            _label.text = GetLabel();
    }

    protected abstract string GetLabel();
    protected abstract void ApplyEffect(SquadController squad);

    private void OnTriggerEnter(Collider other)
    {
        if (_alreadyTriggered) return;

        SquadController squad = other.GetComponentInParent<SquadController>();
        if (squad == null) return;

        // Проверка: лидер действительно ПРОШЁЛ через ворота по X?
        // Сравниваем X лидера и X ворот в мировых координатах
        float gateX   = transform.position.x;
        float leaderX = squad.transform.position.x;
        float deltaX  = Mathf.Abs(leaderX - gateX);

        Debug.Log($"[Gate] gateX={gateX:F2}, leaderX={leaderX:F2}, delta={deltaX:F2}, лимит={_passageHalfWidth}", this);

        if (deltaX > _passageHalfWidth)
        {
            return; // лидер сбоку от ворот
        }

        _alreadyTriggered = true;

        ApplyEffect(squad);
        Debug.Log($"[Gate] {GetLabel()} сработали!", this);

        // Если есть эффект разбития — запускаем его
        GateGlassEffect glass = GetComponent<GateGlassEffect>();
        if (glass != null)
        {
            glass.Shatter();
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
