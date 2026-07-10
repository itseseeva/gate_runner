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

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;

        // Фикс бага "первого удара": если префаб был сохранен выключенным, 
        // первое включение через SetActive(true) вызовет Awake.
        // Если мы получили урон, _targetRatio уже будет < 1, и нам НЕ НУЖНО его прятать.
        if (_targetRatio >= 1f && _root != null) 
        {
            _root.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (_root == null || !_root.activeSelf) return;

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
        if (_fillImage != null)
        {
            // 1. Стандартный способ (работает только если назначен Source Image и Image Type = Filled)
            _fillImage.fillAmount = _displayedRatio;

            // 2. Железобетонный способ (работает ВСЕГДА, даже если Source Image = None)
            // Мы просто сдвигаем правый якорь (Anchor Max X) влево.
            RectTransform fillRect = _fillImage.rectTransform;
            fillRect.anchorMax = new Vector2(_displayedRatio, fillRect.anchorMax.y);
            fillRect.offsetMax = new Vector2(0, fillRect.offsetMax.y); // обнуляем отступ справа


            // Цвет по градиенту (0 = красный, 1 = зелёный)
            if (_hpGradient != null)
                _fillImage.color = _hpGradient.Evaluate(_displayedRatio);
        }

        // Проверяем — пора скрывать?
        if (_hideAtTime > 0f && Time.time >= _hideAtTime)
        {
            _root.SetActive(false);
            _hideAtTime = -1f;
        }
    }

    /// <summary>
    /// Обновляет HP-бар. Если HP уменьшилось — показывает бар на _hideDelay секунд.
    /// Вызывается из Enemy.TakeDamage / Unit.TakeDamage.
    /// </summary>
    public void SetHP(int current, int max)
    {
        if (max <= 0) return;

        _targetRatio = Mathf.Clamp01((float)current / max);

        // Полное HP — скрываем бар
        if (current >= max)
        {
            if (_root != null) _root.SetActive(false);
            _hideAtTime = -1f;
            _displayedRatio = 1f;   // сбрасываем чтобы при следующем показе не было "прыжка"
            return;
        }

        // Получили урон — показываем бар и продлеваем таймер
        if (_root != null && !_root.activeSelf)
            _root.SetActive(true);

        _hideAtTime = Time.time + _hideDelay;
    }
}
