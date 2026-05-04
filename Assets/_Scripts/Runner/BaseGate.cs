using UnityEngine;
using TMPro;

/// <summary>
/// Базовый класс ворот. Дочерние классы реализуют ApplyEffect().
/// </summary>
public abstract class BaseGate : MonoBehaviour
{
    [Header("Визуал")]
    [SerializeField] private TextMeshPro _label;

    protected virtual void Start()
    {
        if (_label != null)
            _label.text = GetLabel();
    }

    /// <summary>Текст на воротах — реализуют дочерние классы.</summary>
    protected abstract string GetLabel();

    /// <summary>Эффект при прохождении — реализуют дочерние классы.</summary>
    protected abstract void ApplyEffect(SquadController squad);

    private void OnTriggerEnter(Collider other)
    {
        SquadController squad = other.GetComponentInParent<SquadController>();
        if (squad == null) return;

        ApplyEffect(squad);
        Debug.Log($"[Gate] {GetLabel()} сработали!", this);

        // Если есть эффект разбития — запускаем его
        GateGlassEffect glass = GetComponent<GateGlassEffect>();
        if (glass != null)
        {
            glass.Shatter();
            // Отключаем сам коллайдер чтобы не сработало повторно
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
