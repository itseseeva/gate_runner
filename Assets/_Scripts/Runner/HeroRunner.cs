using UnityEngine;

/// <summary>
/// Двигает героя автоматически вперёд по оси Z.
/// Скорость настраивается в Inspector.
/// </summary>
public class HeroRunner : MonoBehaviour
{
    [Header("Движение")]
    [Tooltip("Скорость бега в метрах в секунду")]
    [SerializeField] private float _forwardSpeed = 5f;
    
    private void Update()
    {
        // Двигаем героя вперёд каждый кадр.
        // Time.deltaTime даёт независимость от FPS — на любом телефоне скорость одинаковая.
        transform.Translate(Vector3.forward * _forwardSpeed * Time.deltaTime);
    }
}