using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Умный" HP-бар над юнитом или врагом.
/// Скрыт по умолчанию. Появляется при первом уроне,
/// прячется через _hideDelay секунд если урона больше нет.
/// Всегда смотрит на камеру (билборд).
/// Плавно анимирует заполнение и меняет цвет по градиенту (зелёный → красный).
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject _root;       // корневой объект бара (для скрытия)
    [SerializeField] private Image      _fillImage;  // цветная заливка с Fill Method=Horizontal
    [SerializeField] private Image      _bgImage;    // тёмная подложка (опционально)

    [Header("Настройки поведения")]
    [SerializeField] private float _hideDelay = 2.5f;   // через сколько прячется после последнего урона
    [SerializeField] private float _lerpSpeed = 8f;     // скорость плавной анимации бара (больше = быстрее)

    [Header("Цвет по HP")]
    [Tooltip("Градиент цвета: слева (0) = мало HP, справа (1) = много HP")]
    [SerializeField] private Gradient _hpGradient;      // настраивается в Inspector

    private Camera _camera;
    private float  _hideAtTime = -1f;   // когда скрыть (-1 = не показан / не запланировано)
    private float  _targetRatio = 1f;   // куда должен приехать бар (реальное HP / max)
    private float  _displayedRatio = 1f;// что реально показано (плавно догоняет target)

    // Если _root не назначен — бар управляет только _fillImage и _bgImage,
    // НЕ выключая свой gameObject. Это важно для сундуков и других объектов,
    // где HealthBar находится непосредственно на Canvas без отдельного root-объекта.
    private bool _selfManaged => _root == null;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;

        // Скрываем визуал при старте. Если _root назначен — скрываем его.
        // Если нет — скрываем только картинки, НЕ выключая этот gameObject.
        if (_targetRatio >= 1f)
            HideVisual();
    }

    private void LateUpdate()
    {
        // Если _root назначен и выключен — нет смысла работать.
        if (!_selfManaged && (_root == null || !_root.activeSelf)) return;

        // Билборд — поворачиваем бар к камере
        if (_camera == null) _camera = Camera.main;
        if (_camera != null)
            transform.rotation = _camera.transform.rotation;

        // Плавно приближаем displayed к target
        _displayedRatio = Mathf.MoveTowards(
            _displayedRatio,
            _targetRatio,
            _lerpSpeed * Time.deltaTime
        );

        // Применяем к Image
        ApplyFill(_displayedRatio);

        // Проверяем — пора скрывать?
        if (_hideAtTime > 0f && Time.time >= _hideAtTime)
        {
            HideVisual();
            _hideAtTime = -1f;
        }
    }

    /// <summary>Скрывает визуал бара. Если _root назначен — выключает его. Иначе — только картинки.</summary>
    private void HideVisual()
    {
        if (!_selfManaged && _root != null)
        {
            _root.SetActive(false);
        }
        else
        {
            // Режим без root — прячем только картинки, сам gameObject остаётся активным.
            if (_fillImage != null) _fillImage.enabled = false;
            if (_bgImage   != null) _bgImage.enabled   = false;
        }
    }

    /// <summary>Показывает визуал бара.</summary>
    private void ShowVisual()
    {
        if (!_selfManaged && _root != null)
        {
            if (!_root.activeSelf) _root.SetActive(true);
        }
        else
        {
            if (_fillImage != null) _fillImage.enabled = true;
            if (_bgImage   != null) _bgImage.enabled   = true;
        }
    }

    /// <summary>Обновляет fillImage немедленно (не ждёт LateUpdate).</summary>
    private void ApplyFill(float ratio)
    {
        if (_fillImage == null) return;

        _fillImage.fillAmount = ratio;

        RectTransform fillRect = _fillImage.rectTransform;
        fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        fillRect.offsetMax = new Vector2(0, fillRect.offsetMax.y);

        if (_hpGradient != null)
            _fillImage.color = _hpGradient.Evaluate(ratio);
    }

    /// <summary>
    /// Принудительно показывает бар, минуя всю логику HP.
    /// Используется в Breakable, где бар нужно показать явно.
    /// </summary>
    public void ForceShow()
    {
        ShowVisual();
        _hideAtTime = -1f;
    }

    /// <summary>
    /// Обновляет HP-бар. Если HP уменьшилось — показывает бар на _hideDelay секунд.
    /// Вызывается из Enemy.TakeDamage / Unit.TakeDamage / Breakable.TakeDamage.
    /// </summary>
    public void SetHP(int current, int max)
    {
        Debug.Log($"[HealthBar] SetHP: {current}/{max} | _root={(_root!=null?_root.name:"NULL")} | " +
                  $"_selfManaged={_selfManaged} | _fillImage={(_fillImage!=null?_fillImage.name:"NULL")} | " +
                  $"rootActive={(_root!=null?_root.activeSelf.ToString():"n/a")}", this);
        if (max <= 0) return;

        _targetRatio = Mathf.Clamp01((float)current / max);

        // Полное HP — скрываем бар
        if (current >= max)
        {
            HideVisual();
            _hideAtTime    = -1f;
            _displayedRatio = 1f;
            return;
        }

        // Получили урон — показываем бар, обновляем fill немедленно и продлеваем таймер.
        ShowVisual();
        _displayedRatio = _targetRatio; // без плавности при первом показе — без прыжка с 100%
        ApplyFill(_displayedRatio);

        _hideAtTime = Time.time + _hideDelay;
    }
}
