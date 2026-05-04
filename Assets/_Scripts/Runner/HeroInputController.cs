using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет движением героя влево/вправо по дельте касания пальца.
/// Работает и на телефоне (Touch) и в редакторе (мышь).
/// </summary>
public class HeroInputController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float _sensitivity  = 0.02f;  // чувствительность свайпа
    [SerializeField] private float _smoothSpeed  = 12f;    // плавность (lerp)
    [SerializeField] private float _trackHalfWidth = 2f;   // граница дорожки (-2 до +2)

    private float   _targetX;       // куда хотим попасть
    private Vector2 _prevPointerPos; // позиция пальца в прошлом кадре
    private bool    _isPressed;      // палец на экране?

    private void Start()
    {
        _targetX = transform.position.x;
    }

    private void Update()
    {
        HandleInput();
        MoveHero();
    }

    private void HandleInput()
    {
        // ── Телефон (Touch) ──────────────────────────────
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.isPressed)
            {
                Vector2 pos = touch.position.ReadValue();
                if (_isPressed)
                {
                    float delta = pos.x - _prevPointerPos.x;
                    _targetX += delta * _sensitivity;
                }
                _prevPointerPos = pos;
                _isPressed = true;
            }
            else
            {
                _isPressed = false;
            }
            return; // если есть тачскрин — мышь игнорируем
        }

        // ── Редактор / ПК (мышь) ────────────────────────
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                if (_isPressed)
                {
                    float delta = pos.x - _prevPointerPos.x;
                    _targetX += delta * _sensitivity;
                }
                _prevPointerPos = pos;
                _isPressed = true;
            }
            else
            {
                _isPressed = false;
            }
        }
    }

    private void MoveHero()
    {
        // Ограничиваем целевую позицию шириной дорожки
        _targetX = Mathf.Clamp(_targetX, -_trackHalfWidth, _trackHalfWidth);

        // Плавно двигаем героя к целевой позиции
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, _targetX, _smoothSpeed * Time.deltaTime);
        transform.position = pos;
    }
}