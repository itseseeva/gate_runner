using UnityEngine;
using DG.Tweening;

/// <summary>
/// HP-компонент врага. Хранит здоровье и умирает при HP=0.
/// Атака — отдельным компонентом (EnemyMeleeAttack).
/// </summary>
public class Enemy : MonoBehaviour
{
    public static event System.Action<Enemy> OnAnyEnemyDied;
    [Header("Данные врага")]
    [SerializeField] private EnemyDefinitionSO _data;

    [Header("UI")]
    [SerializeField] private HealthBar _healthBar;

    [Header("Отладка (только для чтения)")]
    [SerializeField] private int _currentHP;

    private bool _isDead = false;
    private int  _maxHP;

    public bool IsBoss => false; // TODO: вынести в EnemyDefinitionSO когда будем делать боссов

    private Vector3 _initialScale;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        _isDead    = false;
        _maxHP     = _data != null ? _data.MaxHP : 50;
        _currentHP = _maxHP;

        // Восстанавливаем трансформ (после смерти он был искажён)
        transform.localScale = _initialScale;
        // Rotation — оставляем стартовое значение что было в Prefab
        // Если враги поворачиваются после спавна — оставь как было
        // transform.rotation = Quaternion.identity;

        // Восстанавливаем цвет материала (анимация смерти делала alpha=0)
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
            }
        }

        // Сообщаем бару полное HP — он скроется автоматически
        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);
    }

    /// <summary>
    /// Применяет множитель к Max HP. Вызывается LevelGenerator-ом при спавне.
    /// </summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (_data == null) return;

        int boostedHP = Mathf.RoundToInt(_data.MaxHP * multiplier);
        _maxHP = boostedHP;
        _currentHP = boostedHP;

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);
    }

    /// <summary>Получает урон. showDamageNumber=false для системных смертей (таран, cleanup).</summary>
    public bool TakeDamage(int amount, bool showDamageNumber = true)
    {
        if (_isDead) return false;

        _currentHP -= amount;
        
        // Спавн цифры урона над врагом (только для реального урона от юнитов)
        if (showDamageNumber && DamageNumberPool.Instance != null)
        {
            // Спавним прямо В модели (на её высоте), цифра потом взлетит вверх
            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
            DamageNumberPool.Instance.Spawn(amount, spawnPos, false);
        }

        Debug.Log($"[Enemy] {gameObject.name} получил {amount} урона. HP={_currentHP}", this);

        // Обновляем бар
        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Die();
            return true;
        }
        return false;
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log($"[Enemy] {gameObject.name} погиб!", this);

        OnAnyEnemyDied?.Invoke(this);

        PlayDeathAnimation();
    }

    /// <summary>
    /// Анимация смерти: наклон + сжатие + затухание.
    /// По завершении объект деактивируется.
    /// Best practice "squash & dissolve" — без взрыва, плавно.
    /// </summary>
    private void PlayDeathAnimation()
    {
        // Случайный наклон вбок (как будто падает)
        float fallAngle = Random.Range(60f, 90f) * (Random.value < 0.5f ? -1 : 1);

        // Случайный поворот по Z (более естественно)
        Vector3 targetEuler = transform.eulerAngles + new Vector3(fallAngle, 0f, Random.Range(-20f, 20f));

        Sequence seq = DOTween.Sequence();

        // Параллельно: наклон + сжатие
        seq.Append(transform.DORotate(targetEuler, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InQuad));

        // Затухание материала
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor"))
            {
                Color startColor = mat.GetColor("_BaseColor");
                Color endColor   = new Color(startColor.r, startColor.g, startColor.b, 0f);

                seq.Join(DOVirtual.Color(startColor, endColor, 0.4f, c =>
                {
                    if (mat != null && mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", c);
                }));
            }
        }

        // По окончании — деактивируем
        seq.OnComplete(() =>
        {
            // Перед деактивацией восстанавливаем оригинальный трансформ для следующего использования
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// Смерть от отталкивания — без анимации, просто деактивация после отлёта.
    /// Вызывается KnockbackReceiver когда враг умирает от удара танка.
    /// </summary>
    public void DieFromKnockback()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[Enemy] {gameObject.name} погиб от отталкивания!", this);
        OnAnyEnemyDied?.Invoke(this);
        // Деактивация произойдёт в KnockbackReceiver после завершения отлёта
    }
}
