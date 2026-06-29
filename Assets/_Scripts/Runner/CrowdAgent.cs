using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Агент кучи. Юнит плавно движется к якорю (его место по роли)
/// и расталкивается с близкими соседями — получается живая толпа.
/// Применяется ТОЛЬКО когда юнит в строю (не в бою).
/// Соседей передаёт SquadController — без дорогого Physics.OverlapSphere.
/// </summary>
public class CrowdAgent : MonoBehaviour
{
    [Header("Кучкование")]
    [Tooltip("Скорость движения к якорю")]
    [SerializeField] private float _moveSpeed = 8f;
 
    [Tooltip("На каком расстоянии соседи начинают расталкиваться")]
    [SerializeField] private float _separationRadius = 0.45f;
 
    [Tooltip("Сила расталкивания соседей")]
    [SerializeField] private float _separationForce = 6f;
 
    /// <summary>Целевая точка юнита (мир). Ставит SquadController.</summary>
    public Vector3 Anchor { get; set; }
 
    /// <summary>Множитель радиуса расталкивания — растёт с числом юнитов в куче.</summary>
    public float DensityScale { get; set; } = 1f;
 
    // Стабильное собственное направление "своего места" в куче.
    // Раскидывает якоря по кругу, чтобы юниты формировали круглую кучу,
    // а не залипали в линию из-за симметричного равновесия.
    private Vector2 _personalOffsetDir;
    private bool _dirReady;
 
    private void EnsureDir()
    {
        if (_dirReady) return;
        // Уникальный, но стабильный угол по InstanceID — у каждого юнита свой,
        // распределены равномерно по окружности.
        float seed = (GetInstanceID() * 2.3999632f); // золотой угол в радианах
        _personalOffsetDir = new Vector2(Mathf.Cos(seed), Mathf.Sin(seed));
        _dirReady = true;
    }
 
    /// <summary>
    /// Двигает юнита к якорю + расталкивает от соседей.
    /// neighbors — список позиций соседних юнитов (передаёт SquadController).
    /// </summary>
    public void Step(List<Vector3> neighbors, float dt)
    {
        EnsureDir();
 
        Vector3 pos = transform.position;
 
        // 1. Притяжение к якорю, слегка смещённому в личном направлении.
        // Смещение крошечное (масштаб радиуса) — оно лишь задаёт юниту
        // "свою сторону" кучи, ломая симметрию линии. Кучу держит separation.
        float spread = _separationRadius * DensityScale * 0.5f;
        Vector3 personalAnchor = Anchor + new Vector3(
            _personalOffsetDir.x * spread, 0f, _personalOffsetDir.y * spread);
 
        Vector3 toAnchor = personalAnchor - pos;
 
        // 2. Расталкивание от близких соседей
        Vector3 push = Vector3.zero;
        float effectiveRadius = _separationRadius * DensityScale;
        foreach (Vector3 n in neighbors)
        {
            Vector3 away = pos - n;
            float dist = away.magnitude;
            if (dist > 0.0001f && dist < effectiveRadius)
            {
                // Чем ближе сосед — тем сильнее толчок
                push += away.normalized * (effectiveRadius - dist) / effectiveRadius;
            }
        }
 
        // Складываем силы и двигаемся
        Vector3 velocity = toAnchor * _moveSpeed + push * _separationForce;
        Vector3 next = pos + velocity * dt;
        next.y = Anchor.y; // держим высоту якоря, не уезжаем по Y
 
        transform.position = next;
    }
}