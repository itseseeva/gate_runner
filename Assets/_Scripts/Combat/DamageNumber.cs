using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Цифра урона над врагом. Висит на отдельном prefab с TextMeshPro (World Space).
/// При показе:
/// 1. Появляется с scale 0 → 1.1 → 1 (bounce)
/// 2. Летит вверх на ~1м
/// 3. Затухает (alpha → 0)
/// 4. Возвращается в пул
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    private TextMeshPro _label;
    private Sequence    _activeSequence;

    [Header("Анимация")]
    [SerializeField] private float _flyDuration = 0.6f;
    [SerializeField] private float _flyDistance = 1.5f;
    [SerializeField] private float _appearScale = 1.1f;

    [Header("Цвета")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _critColor   = new Color(1f, 0.85f, 0.2f);

    private void Awake()
    {
        _label = GetComponent<TextMeshPro>();
    }

    /// <summary>Показывает цифру урона в указанной позиции.</summary>
    public void Show(int damage, Vector3 worldPosition, bool isCritical)
    {
        // Останавливаем предыдущую анимацию если была
        _activeSequence?.Kill();

        transform.position = worldPosition;
        transform.localScale = Vector3.zero;

        if (isCritical)
        {
            _label.text     = $"{damage}!";
            _label.color    = _critColor;
            _label.fontSize = 6f;
        }
        else
        {
            _label.text     = damage.ToString();
            _label.color    = _normalColor;
            _label.fontSize = 4f;
        }

        // Поворачиваем к камере (Billboard)
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        // Sequence анимаций
        _activeSequence = DOTween.Sequence();

        // Появление с bounce
        _activeSequence.Append(transform.DOScale(_appearScale, 0.12f).SetEase(Ease.OutQuad));
        _activeSequence.Append(transform.DOScale(1f, 0.08f));

        // Полёт вверх + затухание
        Vector3 endPos = worldPosition + Vector3.up * _flyDistance;
        _activeSequence.Append(transform.DOMove(endPos, _flyDuration).SetEase(Ease.OutQuad));

        // Fade в параллель с полётом
        _activeSequence.Join(_label.DOFade(0f, _flyDuration));

        // По окончании — возврат в пул
        _activeSequence.OnComplete(() =>
        {
            // Сбрасываем alpha для следующего использования
            Color c = _label.color;
            c.a = 1f;
            _label.color = c;

            DamageNumberPool.Instance?.Return(this);
        });
    }

    private void OnDisable()
    {
        _activeSequence?.Kill();
    }
}
