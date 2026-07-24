using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// Обрабатывает тап/клик по зданиям на сцене базы.
/// Через raycast от камеры определяет какое здание выделено.
///
/// События:
/// - OnBuildingSelected — выделили здание
/// - OnSelectionCleared — кликнули в пустоту
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Camera _camera;

    [Tooltip("Слой зданий (опционально, для быстрого raycast)")]
    [SerializeField] private LayerMask _buildingLayer = ~0;  // по умолчанию — все слои

    private BuildingView _currentSelection;

    // События для UI
    public static event Action<BuildingView> OnBuildingSelected;
    public static event Action OnSelectionCleared;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void Update()
    {
        // Проверяем тап / клик в этом кадре
        if (IsTapThisFrame(out Vector2 screenPos))
        {
            // Не реагируем на тапы по UI
            if (IsPointerOverUI()) return;

            HandleTap(screenPos);
        }
    }

    /// <summary>Был ли тап в этом кадре (касание начато или клик нажат).</summary>
    private bool IsTapThisFrame(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;

        // Touch
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var primary = Touchscreen.current.primaryTouch;
            if (primary.press.wasPressedThisFrame)
            {
                screenPos = primary.position.ReadValue();
                return true;
            }
        }

        // Mouse — только моментальный клик (не drag)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    /// <summary>Проверка что палец/курсор над UI элементом.</summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleTap(Vector2 screenPos)
    {
        Ray ray = _camera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _buildingLayer))
        {
            // Ищем BuildingView у объекта или его родителя
            BuildingView view = hit.collider.GetComponentInParent<BuildingView>();
            if (view != null)
            {
                SelectBuilding(view);
                return;
            }
        }

        // Клик в пустоту — снимаем выделение
        ClearSelection();
    }

    private void SelectBuilding(BuildingView view)
    {
        if (_currentSelection == view) return;

        // Снимаем со старого
        if (_currentSelection != null)
            _currentSelection.SetSelected(false);

        _currentSelection = view;
        view.SetSelected(true);

        {}
        OnBuildingSelected?.Invoke(view);
    }

    private void ClearSelection()
    {
        if (_currentSelection == null) return;

        _currentSelection.SetSelected(false);
        _currentSelection = null;

        OnSelectionCleared?.Invoke();
    }
}
