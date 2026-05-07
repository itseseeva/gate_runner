using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Умный" HP-бар над юнитом или врагом.
/// Скрыт по умолчанию. Появляется при первом уроне,
/// прячется через _hideDelay секунд если урона больше нет.
/// Всегда смотрит на камеру (билборд).
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject _root;       // корневой объект бара (для скрытия)
    [SerializeField] private Image      _fillImage;  // красная заливка с Fill Method=Horizontal

    [Header("Настройки")]
    [SerializeField] private float _hideDelay = 2.5f;  // через сколько прячется после последнего урона

    private Camera _camera;
    private float  _hideAtTime = -1f;  // когда скрыть (-1 = не показан / не запланировано)

    private void Awake()
    {
        _camera = Camera.main;

        // По умолчанию скрыт
        if (_root != null) _root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_root == null || !_root.activeSelf) return;

        // Билборд — поворачиваем бар к камере
        if (_camera != null)
        {
            transform.rotation = _camera.transform.rotation;
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

        float ratio = (float)current / max;

        if (_fillImage != null)
            _fillImage.fillAmount = Mathf.Clamp01(ratio);

        // Полное HP — скрываем бар
        if (current >= max)
        {
            if (_root != null) _root.SetActive(false);
            _hideAtTime = -1f;
            return;
        }

        // Получили урон — показываем бар и продлеваем таймер
        if (_root != null && !_root.activeSelf)
            _root.SetActive(true);

        _hideAtTime = Time.time + _hideDelay;
    }
}
