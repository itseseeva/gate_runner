using UnityEngine;
using DG.Tweening;

/// <summary>
/// Цифра урона над врагом — собирается из SpriteRenderer'ов, по одному на разряд.
/// Спрайты дают полный контроль над кернингом: атлас пропорциональный
/// ("1" узкая, "0" широкая), TMP выравнивал бы их по метрикам глифов, которых нет.
/// Прикреплена к цели через LateUpdate — движется вместе с врагом.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private const int MaxDigits = 6;   // до 999999 урона — с запасом

    [Header("Спрайты цифр 0-9")]
    [Tooltip("Строго по порядку: элемент 0 = цифра '0', элемент 9 = цифра '9'")]
    [SerializeField] private Sprite[] _digitSprites = new Sprite[10];

    [Header("Эмодзи стихий")]
    [SerializeField] private Sprite _burnEmoji;
    [SerializeField] private Sprite _freezeEmoji;
    [SerializeField] private Sprite _shockEmoji;

    [Header("Знаки")]
    [Tooltip("Минус перед числом урона")]
    [SerializeField] private Sprite _minusSprite;

    [Tooltip("Восклицательный знак после числа при крите")]
    [SerializeField] private Sprite _critMarkSprite;

    [Tooltip("Зазор между знаком и числом")]
    [SerializeField] private float _signGap = 0.02f;

    [Tooltip("Сдвиг минуса по X. Плюс = вправо, минус = влево.")]
    [SerializeField] private float _signOffsetX = 0f;

    [Tooltip("Сдвиг минуса по Y. Плюс = вверх.")]
    [SerializeField] private float _signOffsetY = 0f;

    [Header("Раскладка")]
    [Tooltip("Зазор между цифрами (мировые единицы). Отрицательный = цифры ближе.")]
    [SerializeField] private float _digitGap = -0.02f;

    [Tooltip("Зазор перед эмодзи.")]
    [SerializeField] private float _emojiGap = 0.05f;

    [Tooltip("Размер эмодзи относительно цифры.")]
    [SerializeField] private float _emojiScale = 0.7f;

    [Tooltip("Вертикальное смещение эмодзи.")]
    [SerializeField] private float _emojiVOffset = 0.05f;

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
    [SerializeField] private float _maxRotation = 15f;

    [Header("Цвета по типам")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _critColor   = Color.white;
    [SerializeField] private Color _burnColor   = Color.white;
    [SerializeField] private Color _freezeColor = Color.white;
    [SerializeField] private Color _shockColor  = Color.white;
    [SerializeField] private Color _healColor   = new Color(0.4f, 1f, 0.5f);

    [Header("Множители размера по типам")]
    [SerializeField] private float _critSizeMul   = 1.6f;
    [SerializeField] private float _burnSizeMul   = 0.7f;
    [SerializeField] private float _normalSizeMul = 1f;

    // ─── Рантайм ────────────────────────────────────────────────
    private SpriteRenderer[] _digitRenderers;
    private SpriteRenderer   _emojiRenderer;
    private SpriteRenderer   _signRenderer;   // минус слева от числа
    private SpriteRenderer   _critRenderer;   // "!" справа от числа
    private Transform        _root;            // контейнер разрядов — его и скейлим

    private Sequence  _activeSequence;
    private Transform _followTarget;
    private Vector3   _followOffset;
    private float     _flyProgress;
    private Vector3   _fallbackPos;
    private int       _accumulatedDamage;

    // Буфер разрядов — переиспользуется, чтобы не аллоцировать на каждом ударе
    private readonly int[] _digitBuffer = new int[MaxDigits];

    public Transform Target => _followTarget;
    public bool IsPlaying => _activeSequence != null && _activeSequence.IsPlaying();

    private void Awake()
    {
        BuildRenderers();
    }

    /// <summary>
    /// Создаёт пул рендереров под разряды один раз при инициализации.
    /// В бою мы только включаем/выключаем их и меняем спрайт — без Instantiate.
    /// </summary>
    private void BuildRenderers()
    {
        _root = new GameObject("Digits").transform;
        _root.SetParent(transform, false);

        _digitRenderers = new SpriteRenderer[MaxDigits];
        for (int i = 0; i < MaxDigits; i++)
        {
            var go = new GameObject($"D{i}");
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.enabled = false;
            _digitRenderers[i] = sr;
        }

        var emojiGo = new GameObject("Emoji");
        emojiGo.transform.SetParent(_root, false);
        _emojiRenderer = emojiGo.AddComponent<SpriteRenderer>();
        _emojiRenderer.enabled = false;

        var signGo = new GameObject("Sign");
        signGo.transform.SetParent(_root, false);
        _signRenderer = signGo.AddComponent<SpriteRenderer>();
        _signRenderer.enabled = false;

        var critGo = new GameObject("CritMark");
        critGo.transform.SetParent(_root, false);
        _critRenderer = critGo.AddComponent<SpriteRenderer>();
        _critRenderer.enabled = false;
    }

    /// <summary>Показывает цифру, прикреплённую к цели. Тип задаёт стиль.</summary>
    public void Show(int damage, Transform target, DamageNumberType type, float sideOffset, float spawnHeight)
    {
        _activeSequence?.Kill();
        _followTarget = target;

        // Случайный хаос по X — цифра появляется сверху, но чуть левее или правее.
        float side = Random.Range(-1f, 1f);
        _followOffset = new Vector3(side * sideOffset, spawnHeight, 0f);

        _flyProgress = 0f;

        Vector3 startPos = (target != null ? target.position : transform.position) + _followOffset;
        transform.position   = startPos;
        _fallbackPos         = startPos;
        transform.localScale = Vector3.zero;

        _accumulatedDamage = damage;

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

        // Откатываем полёт назад, чтобы цифра дольше висела при серии ударов
        _flyProgress = Mathf.Clamp01(_flyProgress - 0.25f);

        PlayAnimation(sizeMul, false);
    }

    /// <summary>Применяет цвет/размер по типу и раскладывает разряды.</summary>
    private float UpdateStyle(DamageNumberType type, int currentDamage)
    {
        float sizeMul = _normalSizeMul;
        Color color;
        Sprite emoji = null;

        switch (type)
        {
            case DamageNumberType.Critical:
                color = _critColor;
                sizeMul = _critSizeMul;
                break;
            case DamageNumberType.Burn:
                color = _burnColor;
                sizeMul = _burnSizeMul;
                emoji = _burnEmoji;
                break;
            case DamageNumberType.Freeze:
                color = _freezeColor;
                emoji = _freezeEmoji;
                break;
            case DamageNumberType.Shock:
                color = _shockColor;
                emoji = _shockEmoji;
                break;
            case DamageNumberType.Heal:
                color = _healColor;
                break;
            default:
                color = _normalColor;
                break;
        }

        LayoutDigits(currentDamage, color, emoji, type);
        return sizeMul;
    }

    /// <summary>
    /// Раскладывает [минус][цифры][!][эмодзи] в один ряд, центрируя по локальному нулю.
    /// Шаг = реальная ширина спрайта + зазор. Строк не создаём — только арифметика.
    /// </summary>
    private void LayoutDigits(int value, Color color, Sprite emoji, DamageNumberType type)
    {
        value = Mathf.Abs(value);

        // Минус — на всём, кроме хила. Плюса в атласе нет, хил отличается цветом.
        Sprite sign = (type == DamageNumberType.Heal) ? null : _minusSprite;
        Sprite critMark = (type == DamageNumberType.Critical) ? _critMarkSprite : null;

        // ── Разбор числа на разряды справа налево ────────────────
        int count = 0;
        if (value == 0)
        {
            _digitBuffer[0] = 0;
            count = 1;
        }
        else
        {
            while (value > 0 && count < MaxDigits)
            {
                _digitBuffer[count++] = value % 10;
                value /= 10;
            }
        }

        // ── Считаем полную ширину для центровки ──────────────────
        float totalWidth = 0f;

        if (sign != null)
            totalWidth += sign.bounds.size.x + _signGap;

        for (int i = count - 1; i >= 0; i--)
        {
            Sprite s = _digitSprites[_digitBuffer[i]];
            if (s == null) continue;
            totalWidth += s.bounds.size.x + _digitGap;
        }
        totalWidth -= _digitGap;   // последний зазор лишний

        if (critMark != null)
            totalWidth += _signGap + critMark.bounds.size.x;

        if (emoji != null)
            totalWidth += _emojiGap + emoji.bounds.size.x * _emojiScale;

        float cursor = -totalWidth * 0.5f;

        // ── Минус ────────────────────────────────────────────────
        if (sign != null)
        {
            _signRenderer.enabled = true;
            _signRenderer.sprite  = sign;
            _signRenderer.color   = color;

            float w = sign.bounds.size.x;
            _signRenderer.transform.localPosition =
                new Vector3(cursor + w * 0.5f + _signOffsetX, _signOffsetY, 0f);   // ← вот тут
            _signRenderer.transform.localScale    = Vector3.one;

            cursor += w + _signGap;
        }
        else
        {
            _signRenderer.enabled = false;
        }

        // ── Цифры слева направо ──────────────────────────────────
        int r = 0;
        for (int i = count - 1; i >= 0; i--, r++)
        {
            Sprite s = _digitSprites[_digitBuffer[i]];
            SpriteRenderer sr = _digitRenderers[r];

            if (s == null)
            {
                sr.enabled = false;
                continue;
            }

            sr.enabled = true;
            sr.sprite  = s;
            sr.color   = color;

            float w = s.bounds.size.x;
            sr.transform.localPosition = new Vector3(cursor + w * 0.5f, 0f, 0f);
            sr.transform.localScale    = Vector3.one;

            cursor += w + _digitGap;
        }

        for (; r < MaxDigits; r++)
            _digitRenderers[r].enabled = false;

        cursor -= _digitGap;   // откатываем лишний зазор после последней цифры

        // ── Восклицательный знак ─────────────────────────────────
        if (critMark != null)
        {
            _critRenderer.enabled = true;
            _critRenderer.sprite  = critMark;
            _critRenderer.color   = color;

            float w = critMark.bounds.size.x;
            _critRenderer.transform.localPosition = new Vector3(cursor + _signGap + w * 0.5f, 0f, 0f);
            _critRenderer.transform.localScale    = Vector3.one;

            cursor += _signGap + w;
        }
        else
        {
            _critRenderer.enabled = false;
        }

        // ── Эмодзи ───────────────────────────────────────────────
        if (emoji != null)
        {
            _emojiRenderer.enabled = true;
            _emojiRenderer.sprite  = emoji;
            _emojiRenderer.color   = Color.white;   // эмодзи не красим — у него свой цвет

            float ew = emoji.bounds.size.x * _emojiScale;
            _emojiRenderer.transform.localPosition =
                new Vector3(cursor + _emojiGap + ew * 0.5f, _emojiVOffset, 0f);
            _emojiRenderer.transform.localScale = Vector3.one * _emojiScale;
        }
        else
        {
            _emojiRenderer.enabled = false;
        }
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
        if (remainingDuration < 0.1f) remainingDuration = 0.1f;

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

        Vector3 basePos;
        if (_followTarget != null && _followTarget.gameObject.activeInHierarchy)
        {
            basePos = _followTarget.position + _followOffset;
            _fallbackPos = basePos;
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
