using UnityEngine;

/// <summary>
/// Сундук с HP. IDamageable — снаряды бьют его как врага.
/// Работает через пул (EnemyPool): OnEnable оживляет сундук и бар при выдаче из пула.
/// HP=0: анимация Open, VFX, награда, возврат в пул.
/// </summary>
[RequireComponent(typeof(WorldScroller))]
public class Breakable : MonoBehaviour, IDamageable
{
    [Header("Прочность")]
    [SerializeField] private int _maxHP = 100;

    [Header("Анимация")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openTrigger = "Open";

    [Header("Эффекты")]
    [SerializeField] private GameObject _breakEffect;

    [Header("UI")]
    [SerializeField] private HealthBar _healthBar;

    private int  _currentHP;
    private bool _isDead;

    public bool IsDead => _isDead;

    private void OnEnable()
    {
        _isDead    = false;
        _currentHP = _maxHP;
        if (_animator == null) _animator = GetComponent<Animator>();

        // Оживляем при выдаче из пула: включаем компоненты движения.
        WorldScroller scroller = GetComponent<WorldScroller>();
        if (scroller != null) scroller.enabled = true;

        Collider col = GetComponentInChildren<Collider>();
        if (col != null) col.enabled = true;

        // Оживляем бар: включаем объект и всех детей, сообщаем полное HP.
        if (_healthBar != null)
        {
            _healthBar.gameObject.SetActive(true);
            foreach (Transform child in _healthBar.transform)
                child.gameObject.SetActive(true);
            _healthBar.SetHP(_currentHP, _maxHP);
        }
    }

    public bool TakeDamage(int amount, bool showDamageNumber = true,
                           DamageNumberType numberType = DamageNumberType.Normal)
    {
        if (_isDead) return false;

        _currentHP -= amount;

        if (_healthBar != null)
        {
            if (!_healthBar.gameObject.activeSelf)
                _healthBar.gameObject.SetActive(true);
            _healthBar.SetHP(Mathf.Max(0, _currentHP), _maxHP);
        }

        if (_currentHP <= 0)
        {
            Open();
            return true;
        }
        return false;
    }

    private void Open()
    {
        _isDead = true;

        if (_animator != null)
            _animator.SetTrigger(_openTrigger);

        if (_breakEffect != null && VfxPool.Instance != null)
            VfxPool.Instance.Spawn(transform.position, Quaternion.identity, _breakEffect);

        if (_healthBar != null)
            _healthBar.gameObject.SetActive(false);

        Debug.Log("[Breakable] Сундук открыт — награда (заглушка).", this);
    }
}
