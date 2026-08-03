using UnityEngine;

/// <summary>
/// Универсальный компонент вращения объекта.
/// Вешается на любой пикап (стихия, монета, лутбокс) — крутит его вокруг заданной оси.
/// Лёгкая операция, поэтому Update здесь оправдан (нет поиска, нет аллокаций).
/// </summary>
public class Rotator : MonoBehaviour
{
    [Header("Настройки вращения")]
    [Tooltip("Ось вращения. (0,1,0) = вокруг вертикали, как пикапы в GTA.")]
    [SerializeField] private Vector3 _axis = Vector3.up;

    [Tooltip("Скорость вращения в градусах в секунду.")]
    [SerializeField] private float _degreesPerSecond = 90f;

    [Tooltip("Крутить в локальных координатах (обычно да).")]
    [SerializeField] private bool _useLocalSpace = true;

    private void Update()
    {
        // deltaTime делает скорость независимой от FPS: на 30 и на 120 кадрах крутится одинаково.
        float step = _degreesPerSecond * Time.deltaTime;
        Space space = _useLocalSpace ? Space.Self : Space.World;
        transform.Rotate(_axis, step, space);
    }
}
