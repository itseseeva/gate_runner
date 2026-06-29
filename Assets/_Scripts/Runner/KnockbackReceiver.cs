using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент отталкивания. Вешается на врага.
/// Если враг умирает от удара танка — отлетает вместо обычной смерти.
/// </summary>
public class KnockbackReceiver : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Дистанция отлёта в метрах")]
    [SerializeField] private float _maxDistance = 6f;

    [Tooltip("Скорость отлёта")]
    [SerializeField] private float _knockbackSpeed = 12f;

    private bool _isBeingKnockedBack = false;
    private WorldScroller _worldScroller;
    private Enemy _enemy;

    private void Awake()
    {
        _worldScroller = GetComponent<WorldScroller>();
        _enemy         = GetComponent<Enemy>();
    }

    private void OnDisable()
    {
        _isBeingKnockedBack = false;
        if (_worldScroller != null) _worldScroller.Resume();
    }

    /// <summary>
    /// Вызывается при ударе.
    /// killedByHit = true если этот удар убил врага.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float distance, bool killedByHit)
    {
        if (_isBeingKnockedBack) return;
        _isBeingKnockedBack = true;
        StartCoroutine(KnockbackCoroutine(direction, distance, killedByHit));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float distance, bool killedByHit)
    {
        // Если враг убит — перехватываем смерть
        if (killedByHit && _enemy != null)
            _enemy.DieFromKnockback();

        if (_worldScroller != null) _worldScroller.Stop();

        Vector3 startPos  = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        float   duration  = distance / _knockbackSpeed;
        float   elapsed   = 0f;

        Debug.Log($"[Knockback Debug] {gameObject.name} flying from {startPos} to {targetPos}. Direction: {direction}");

        while (elapsed < duration)
        {
            if (!gameObject.activeSelf)
            {
                if (_worldScroller != null) _worldScroller.Resume();
                _isBeingKnockedBack = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (_worldScroller != null) _worldScroller.Resume();
        _isBeingKnockedBack = false;

        // Если убит — деактивируем после отлёта
        if (killedByHit)
            gameObject.SetActive(false);

        Debug.Log($"[KnockbackReceiver] {gameObject.name} отлетел на {distance:F1}м", this);
    }
}
