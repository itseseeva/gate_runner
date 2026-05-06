using UnityEngine;

/// <summary>
/// Melee-враг. Едет к отряду через WorldScroller.
/// Когда близко — тянется к центру отряда по X.
/// При касании юнита — наносит урон и погибает.
/// </summary>
[RequireComponent(typeof(WorldScroller))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Наведение на отряд")]
    // TODO: вынести в EnemyDefinitionSO при балансировке
    [SerializeField] private float _trackingRange  = 8f;  // с какой дистанции начинает тянуться
    [SerializeField] private float _trackingSpeed  = 3f;  // скорость смещения по X
    [SerializeField] private int   _damage         = 20;  // урон при таране

    private SquadController _squad;
    private bool            _isDead = false;

    private void Start()
    {
        _squad = FindAnyObjectByType<SquadController>();

        if (_squad == null)
            Debug.LogError("[EnemyMeleeAttack] SquadController не найден!", this);
    }

    private void Update()
    {
        if (_isDead) return;
        if (_squad == null) return;

        // Дистанция до отряда по Z
        float distZ = transform.position.z - _squad.transform.position.z;

        // Далеко — просто едем прямо (WorldScroller делает это сам)
        if (distZ > _trackingRange) return;

        // Близко — тянемся к ближайшему юниту по X
        Unit nearest = _squad.GetNearestUnitByX(transform.position.x);
        if (nearest == null) return;

        float targetX  = nearest.transform.position.x;
        float currentX = transform.position.x;
        float newX     = Mathf.MoveTowards(currentX, targetX, _trackingSpeed * Time.deltaTime);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDead) return;

        // Попали в юнита?
        Unit unit = other.GetComponent<Unit>();
        if (unit == null) return;

        Debug.Log($"[EnemyMelee] {gameObject.name} таранит {unit.gameObject.name}!", this);

        // Наносим урон
        bool killed = unit.TakeDamage(_damage);
        if (killed)
        {
            _squad?.OnUnitDied(unit);
        }

        // Враг погибает после тарана
        Die();
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log($"[EnemyMelee] {gameObject.name} погиб после тарана!", this);
        gameObject.SetActive(false);
    }
}
