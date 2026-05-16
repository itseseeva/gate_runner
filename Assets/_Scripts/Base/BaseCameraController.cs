using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление камерой на базе через новый Input System.
/// - 1 палец / ЛКМ + drag = pan (перемещение)
/// - 2 пальца pinch / scroll wheel = zoom
///
/// Лимиты удерживают камеру в пределах базы.
/// </summary>
public class BaseCameraController : MonoBehaviour
{
    [Header("Pan (перемещение)")]
    [SerializeField] private float _panSpeed = 0.02f;
    [SerializeField] private Vector2 _panMinXZ = new Vector2(-15, -15);
    [SerializeField] private Vector2 _panMaxXZ = new Vector2( 15,  15);

    [Header("Zoom (приближение)")]
    [SerializeField] private float _zoomSpeedTouch = 0.05f;
    [SerializeField] private float _zoomSpeedMouse = 0.5f;
    [SerializeField] private float _zoomMin = 7f;
    [SerializeField] private float _zoomMax = 20f;

    private float _currentZoom;
    private float _lastPinchDistance;
    private Vector2 _lastMousePosition;
    private bool _isMouseDragging;

    private void Start()
    {
        _currentZoom = transform.position.y;
    }

    private void Update()
    {
        HandleTouch();
        HandleMouse();
    }

    // ─── Touch (мобильный) ──────────────────────────────────

    private void HandleTouch()
    {
        if (Touchscreen.current == null) return;

        int activeTouches = 0;
        UnityEngine.InputSystem.LowLevel.TouchState t0 = default, t1 = default;

        // Собираем активные касания
        for (int i = 0; i < Touchscreen.current.touches.Count && activeTouches < 2; i++)
        {
            var touch = Touchscreen.current.touches[i];
            if (touch.press.isPressed)
            {
                if (activeTouches == 0) t0 = touch.ReadValue();
                else if (activeTouches == 1) t1 = touch.ReadValue();
                activeTouches++;
            }
        }

        if (activeTouches == 1)
        {
            // 1 палец — pan
            Vector2 delta = t0.delta;
            PanCamera(delta);
        }
        else if (activeTouches == 2)
        {
            // 2 пальца — pinch zoom
            float currentDistance = Vector2.Distance(t0.position, t1.position);

            // Первый кадр после касания вторым пальцем
            if (Mathf.Approximately(_lastPinchDistance, 0f))
            {
                _lastPinchDistance = currentDistance;
                return;
            }

            float deltaDistance = currentDistance - _lastPinchDistance;
            ZoomCamera(-deltaDistance * _zoomSpeedTouch);
            _lastPinchDistance = currentDistance;
        }
        else
        {
            _lastPinchDistance = 0f;
        }
    }

    // ─── Mouse (для редактора) ──────────────────────────────

    private void HandleMouse()
    {
        if (Mouse.current == null) return;

        // Drag ЛКМ = pan
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 currentPos = Mouse.current.position.ReadValue();

            if (!_isMouseDragging)
            {
                _isMouseDragging = true;
                _lastMousePosition = currentPos;
            }
            else
            {
                Vector2 delta = currentPos - _lastMousePosition;
                PanCamera(delta);
                _lastMousePosition = currentPos;
            }
        }
        else
        {
            _isMouseDragging = false;
        }

        // Scroll wheel = zoom
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ZoomCamera(-scroll * _zoomSpeedMouse * 0.01f);
        }
    }

    // ─── Применение движения ────────────────────────────────

    private void PanCamera(Vector2 screenDelta)
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 move = -right * screenDelta.x * _panSpeed - forward * screenDelta.y * _panSpeed;
        Vector3 newPos = transform.position + move;

        newPos.x = Mathf.Clamp(newPos.x, _panMinXZ.x, _panMaxXZ.x);
        newPos.z = Mathf.Clamp(newPos.z, _panMinXZ.y, _panMaxXZ.y);

        transform.position = newPos;
    }

    private void ZoomCamera(float delta)
    {
        _currentZoom = Mathf.Clamp(_currentZoom + delta, _zoomMin, _zoomMax);

        Vector3 pos = transform.position;
        pos.y = _currentZoom;
        pos.z = transform.position.z + delta * 0.5f;
        transform.position = pos;
    }
}
