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
    [SerializeField] private float _flyDuration = 0.85f;
    [SerializeField] private float _flyDistance = 1.4f;
    [Tooltip("Насколько цифра увеличивается в момент punch. Больше = резче удар.")]
    [SerializeField] private float _appearBounce = 2.2f;

    [Tooltip("Длительность удара (появление). Меньше = резче.")]
    [SerializeField] private float _punchDuration = 0.06f;

    [Tooltip("Время отпружинивания к нормальному размеру.")]
    [SerializeField] private float _settleDuration = 0.18f;
    [SerializeField] private float _maxRotation  = 15f;



    // Все цифры — ОРАНЖЕВЫЕ (единый визуал урона).
    // Стихия обозначается эмодзи справа от цифры, а не цветом.
    [Header("Цвета по типам (все оранжевые — стихию показываем через эмодзи)")]
    [SerializeField] private Color _normalColor = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _critColor   = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _burnColor   = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _freezeColor = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _shockColor  = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color _healColor   = new Color(0.4f, 1f, 0.5f);   // лечение оставляю зелёным

    [Header("Эмодзи стихий (перетащи сюда свои Sprite Assets)")]
    [SerializeField] private TMP_SpriteAsset _burnEmoji;
    [SerializeField] private TMP_SpriteAsset _freezeEmoji;
    [SerializeField] private TMP_SpriteAsset _shockEmoji;

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

    public Transform Target => _followTarget;
    public bool IsPlaying => _activeSequence != null && _activeSequence.IsPlaying();

    private int _accumulatedDamage;

    /// <summary>Показывает цифру, прикреплённую к цели. Тип задаёт стиль.</summary>
    public void Show(int damage, Transform target, DamageNumberType type, float sideOffset, float spawnHeight)
    {
        _activeSequence?.Kill();
        _followTarget = target;

        // Случайный хаос по X — цифра появляется сверху, но чуть левее или правее.
        // -1..+1 в единицах sideOffset. То есть при sideOffset = 0.25 разброс ±0.25м.
        float side = Random.Range(-1f, 1f);

        _followOffset = new Vector3(
            side * sideOffset,
            spawnHeight,
            0f
        );

        _flyProgress  = 0f;

        // Стартовая позиция
        Vector3 startPos = (target != null ? target.position : transform.position) + _followOffset;
        transform.position   = startPos;
        _fallbackPos         = startPos;
        transform.localScale = Vector3.zero;

        _accumulatedDamage = damage;

        // Стиль по типу (с поддержкой эмодзи)
        float sizeMul = UpdateStyle(type, _accumulatedDamage);

        // Поворот к камере + случайный tilt
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
            transform.Rotate(0f, 0f, Random.Range(-_maxRotation, _maxRotation), Space.Self);
        }

        PlayAnimation(sizeMul, true);
    }

    public void AddDamage(int extraDamage, DamageNumberType newType)
    {
        _accumulatedDamage += extraDamage;
        float sizeMul = UpdateStyle(newType, _accumulatedDamage);

        // Откатываем полёт немного назад, чтобы цифра дольше висела в воздухе при серии ударов
        _flyProgress = Mathf.Clamp01(_flyProgress - 0.25f);

        // Перезапускаем анимацию вздрагивания
        PlayAnimation(sizeMul, false);
    }

    private float UpdateStyle(DamageNumberType type, int currentDamage)
    {
        float sizeMul = _normalSizeMul;
        string suffix = "";
        _label.spriteAsset = null;

        // Формируем тег для эмодзи с учетом ползунков из ПУЛА (чтобы менять их в реальном времени)
        float spacing = DamageNumberPool.Instance != null ? DamageNumberPool.Instance.EmojiSpacing : 0.5f;
        float vOffset = DamageNumberPool.Instance != null ? DamageNumberPool.Instance.EmojiVOffset : 0.25f;
        float size = DamageNumberPool.Instance != null ? DamageNumberPool.Instance.EmojiSize : 65f;

        string spaceStr = spacing.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string vOffsetStr = vOffset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int sizeInt = Mathf.RoundToInt(size);
        string emojiTag = $"<space={spaceStr}em><voffset={vOffsetStr}em><size={sizeInt}%><sprite index=0></size>";

        switch (type)
        {
            case DamageNumberType.Normal:
                _label.color = _normalColor;
                break;
            case DamageNumberType.Critical:
                _label.color = _critColor;
                sizeMul = _critSizeMul;
                suffix = "!";
                break;
            case DamageNumberType.Burn:
                _label.color = _burnColor;
                sizeMul = _burnSizeMul;
                _label.spriteAsset = _burnEmoji;
                suffix = emojiTag;
                break;
            case DamageNumberType.Freeze:
                _label.color = _freezeColor;
                _label.spriteAsset = _freezeEmoji;
                suffix = emojiTag;
                break;
            case DamageNumberType.Shock:
                _label.color = _shockColor;
                _label.spriteAsset = _shockEmoji;
                suffix = emojiTag;
                break;
            case DamageNumberType.Heal:
                _label.color = _healColor;
                break;
        }

        // Делаем саму цифру жирной с помощью тега <b>
        if (type == DamageNumberType.Heal)
            _label.text = $"<b>+{currentDamage}</b>";
        else
            _label.text = $"<b>-{currentDamage}</b>" + suffix;

        return sizeMul;
    }

    private void PlayAnimation(float sizeMul, bool isNew)
    {
        _activeSequence?.Kill();
        if (isNew) transform.localScale = Vector3.zero;

        float finalScale  = sizeMul;
        float bounceScale = _appearBounce * sizeMul;

        _activeSequence = DOTween.Sequence();

        // PUNCH: резкий удар в большой scale за очень короткое время
        _activeSequence.Append(
            transform.DOScale(bounceScale, _punchDuration)
                     .SetEase(Ease.OutCubic)
        );
        _activeSequence.Append(
            transform.DOScale(finalScale, _settleDuration)
                     .SetEase(Ease.OutElastic, 1.2f, 0.4f)
        );

        // Полёт вверх (продолжаем с текущего _flyProgress)
        float remainingDuration = _flyDuration * (1f - _flyProgress);
        if (remainingDuration < 0.1f) remainingDuration = 0.1f; // минимум 0.1с чтобы успеть долететь

        _activeSequence.Append(
            DOTween.To(() => _flyProgress, x => _flyProgress = x, 1f, remainingDuration)
                   .SetEase(Ease.OutQuad)
        );

        // POP-OUT: резкий scale → 0 в самом конце полёта
        _activeSequence.Join(
            transform.DOScale(0f, remainingDuration * 0.3f)
                     .SetDelay(remainingDuration * 0.7f)
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
        if (_followTarget != null && _followTarget.gameObject.activeInHierarchy)
        {
            basePos = _followTarget.position + _followOffset;
            _fallbackPos = basePos; // запоминаем последнюю живую позицию
        }
        else
        {
            basePos = _fallbackPos;
        }

        transform.position = basePos + new Vector3(0f, _flyDistance * _flyProgress, 0f);
    }

    private void OnDisable()
    {
        _activeSequence?.Kill();
        _followTarget = null;
        _flyProgress = 0f;
    }
}
