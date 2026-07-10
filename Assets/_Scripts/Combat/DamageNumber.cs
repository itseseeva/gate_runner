using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Цифра урона над врагом (World Space TextMeshPro).
/// Прикреплена к цели через LateUpdate — движется вместе с врагом.
/// Спавнится со смещением ВЛЕВО или ВПРАВО от центра — не сливается в столбик.
/// Стилизуется по DamageNumberType.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    private TextMeshPro _label;
    private Sequence    _activeSequence;

    [Header("Анимация")]
    [Tooltip("Длительность полёта. Общее время жизни цифры = _flyDuration + 0.15 (pop-in).")]
    [SerializeField] private float _flyDuration = 0.35f;
    [SerializeField] private float _flyDistance = 1.4f;
    [SerializeField] private float _appearBounce = 1.5f;  // сильнее overshoot
    [SerializeField] private float _maxRotation  = 15f;



    [Header("Цвета по типам")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _critColor   = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _burnColor   = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _freezeColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private Color _shockColor  = new Color(0.75f, 0.5f, 1f);
    [SerializeField] private Color _healColor   = new Color(0.4f, 1f, 0.5f);

    [Header("Множители размера по типам")]
    [SerializeField] private float _critSizeMul   = 1.6f;
    [SerializeField] private float _burnSizeMul   = 0.7f;
    [SerializeField] private float _normalSizeMul = 1f;

    // ─── Следование за целью ────────────────────────────────────
    private Transform _followTarget;
    private Vector3   _followOffset;    // локальное смещение от таргета (side, high, 0)
    private float     _flyProgress;     // 0 → 1, тикает через DOTween
    private Vector3   _fallbackPos;     // куда упасть, если таргет исчез

    private void Awake()
    {
        _label = GetComponent<TextMeshPro>();
    }

    /// <summary>Показывает цифру, прикреплённую к цели. Тип задаёт стиль.</summary>
    public void Show(int damage, Transform target, DamageNumberType type, float sideOffset, float spawnHeight)
    {
        _activeSequence?.Kill();

        // 80% — сверху над головой (side = 0), 20% — слева/справа
        float side;
        if (Random.value < 0.8f)
        {
            side = 0f; // сверху
        }
        else
        {
            side = Random.value < 0.5f ? -1f : 1f; // редко — вбок
        }

        _followOffset = new Vector3(
            side * sideOffset + Random.Range(-0.05f, 0.05f),
            spawnHeight,
            0f
        );

        _followTarget = target;
        _flyProgress  = 0f;

        // Стартовая позиция
        Vector3 startPos = (target != null ? target.position : transform.position) + _followOffset;
        transform.position   = startPos;
        _fallbackPos         = startPos;
        transform.localScale = Vector3.zero;

        // Стиль по типу
        (string text, Color color, float sizeMul) = GetStyle(damage, type);
        _label.text         = text;
        _label.color        = color;

        // Поворот к камере + случайный tilt
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
            transform.Rotate(0f, 0f, Random.Range(-_maxRotation, _maxRotation), Space.Self);
        }

        float finalScale  = sizeMul;
        float bounceScale = _appearBounce * sizeMul;

        _activeSequence = DOTween.Sequence();

        // POP-IN: 0 → bounce → normal (укоротили под 0.5с общей длительности)
        _activeSequence.Append(transform.DOScale(bounceScale, 0.1f).SetEase(Ease.OutBack, 3f));
        _activeSequence.Append(transform.DOScale(finalScale, 0.05f).SetEase(Ease.OutQuad));

        // Полёт вверх (fade убран — исчезновение через POP-OUT)
        _activeSequence.Append(
            DOTween.To(() => _flyProgress, x => _flyProgress = x, 1f, _flyDuration)
                   .SetEase(Ease.OutQuad)
        );

        // POP-OUT: резкий scale → 0 в самом конце полёта
        _activeSequence.Join(
            transform.DOScale(0f, _flyDuration * 0.3f)
                     .SetDelay(_flyDuration * 0.7f)
                     .SetEase(Ease.InBack)
        );

        _activeSequence.OnComplete(() =>
        {
            _followTarget = null;
            DamageNumberPool.Instance?.Return(this);
        });
    }

    /// <summary>
    /// Обновляем позицию ПОСЛЕ движения врага (LateUpdate),
    /// чтобы цифра не отставала на кадр.
    /// </summary>
    private void LateUpdate()
    {
        if (_flyProgress <= 0f && _followTarget == null) return;

        // База: позиция врага (если жив) или последняя известная точка
        Vector3 basePos;
        if (_followTarget != null && _followTarget.gameObject.activeSelf)
        {
            basePos = _followTarget.position + _followOffset;
            _fallbackPos = basePos; // запоминаем последнюю живую позицию
        }
        else
        {
            basePos = _fallbackPos;
        }

        // Полёт вверх поверх базовой позиции
        transform.position = basePos + new Vector3(0f, _flyDistance * _flyProgress, 0f);
    }

    private (string text, Color color, float sizeMul) GetStyle(int damage, DamageNumberType type)
    {
        return type switch
        {
            DamageNumberType.Critical => ($"{damage}!", _critColor,   _critSizeMul),
            DamageNumberType.Burn     => ($"{damage}",  _burnColor,   _burnSizeMul),
            DamageNumberType.Freeze   => ($"{damage}",  _freezeColor, _normalSizeMul),
            DamageNumberType.Shock    => ($"{damage}",  _shockColor,  _normalSizeMul),
            DamageNumberType.Heal     => ($"+{damage}", _healColor,   _normalSizeMul),
            _                         => ($"{damage}",  _normalColor, _normalSizeMul),
        };
    }

    private void OnDisable()
    {
        _activeSequence?.Kill();
        _followTarget = null;
        _flyProgress = 0f;
    }
}
