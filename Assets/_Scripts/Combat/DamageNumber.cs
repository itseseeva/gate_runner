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
    [SerializeField] private float _flyDuration = 0.5f;     // быстрее
    [SerializeField] private float _flyDistance = 1.2f;     // длиннее полёт
    [SerializeField] private float _appearScale = 1.05f;    // меньше bounce

    [Header("Цвета")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _critColor   = new Color(1f, 0.85f, 0.2f);

    private void Awake()
    {
        _label = GetComponent<TextMeshPro>();
    }

    public void Show(int damage, Vector3 worldPosition, bool isCritical)
    {
        _activeSequence?.Kill();

        // Случайное смещение появления (почти 0)
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.05f, 0.05f),
            0f,
            Random.Range(-0.05f, 0.05f)
        );
        Vector3 startPos = worldPosition + randomOffset;

        transform.position = startPos;
        transform.localScale = Vector3.zero;

        if (isCritical)
        {
            _label.text     = $"{damage}!";
            _label.color    = _critColor;
        }
        else
        {
            _label.text     = damage.ToString();
            _label.color    = _normalColor;
        }

        // Поворачиваем к камере
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        // Случайный угол полёта — не строго вверх, а вверх + чуть в сторону
        float angleX = Random.Range(-0.5f, 0.5f);
        Vector3 endPos = startPos + new Vector3(angleX, _flyDistance, 0f);

        _activeSequence = DOTween.Sequence();

        // Резкий выстрел вверх с быстрым появлением
        _activeSequence.Append(transform.DOScale(_appearScale, 0.08f).SetEase(Ease.OutBack));
        _activeSequence.Append(transform.DOScale(1f, 0.05f));

        _activeSequence.Append(transform.DOMove(endPos, _flyDuration).SetEase(Ease.OutQuad));
        _activeSequence.Join(_label.DOFade(0f, _flyDuration).SetEase(Ease.InQuad));

        _activeSequence.OnComplete(() =>
        {
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
