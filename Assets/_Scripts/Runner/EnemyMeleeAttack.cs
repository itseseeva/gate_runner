using UnityEngine;

/// <summary>
/// Melee-враг. Едет к отряду через WorldScroller.
/// Когда близко — тянется к ближайшему юниту, но расходится с другими врагами.
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

    [Header("Отталкивание от других врагов")]
    [SerializeField] private float _separationRadius = 0.7f;  // на каком расстоянии начинаем расходиться
    [SerializeField] private float _separationForce  = 4f;    // сила расталкивания

    [Header("Хаос")]
    [SerializeField] private float _wobbleAmount    = 0.3f;  // амплитуда покачивания по X
    [SerializeField] private float _wobbleSpeed     = 2f;    // скорость покачивания
    [SerializeField] private float _lazyChance      = 0.3f;  // шанс "лениться" в каждом цикле
    [SerializeField] private float _lazyDuration    = 0.5f;  // как долго ленится за раз
    [SerializeField] private float _lazyCheckPeriod = 1f;    // как часто решает "ленюсь или нет"

    private SquadController _squad;
    private bool            _isDead = false;
    private float           _personalSpeedFactor = 1f; // у каждого врага своя "лень"

    // Внутренние переменные для хаоса
    private float _wobblePhase;       // личная фаза синуса
    private float _nextLazyCheck;     // когда следующая проверка лени
    private float _lazyUntil;         // до какого времени ленюсь

    // Статический список всех живых врагов — для быстрого поиска соседей
    private static readonly System.Collections.Generic.List<EnemyMeleeAttack> _allEnemies = new();

    private void OnEnable()
    {
        _allEnemies.Add(this);
        _personalSpeedFactor = Random.Range(0.7f, 1.3f);

        // Случайная фаза покачивания — все враги качаются "не в ногу"
        _wobblePhase = Random.Range(0f, Mathf.PI * 2f);

        _nextLazyCheck = 0f;
        _lazyUntil     = 0f;
    }

    private void OnDisable()
    {
        _allEnemies.Remove(this);
    }

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

        // Замираем при Game Over / Victory
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        float distZ = transform.position.z - _squad.transform.position.z;
        if (distZ < 0f) return;

        // ─── Множитель скорости (для статуса Frozen) ──────────────
        // Берём из WorldScroller — он же используется для движения по Z.
        float speedMul = 1f;
        var scroller = GetComponent<WorldScroller>();
        if (scroller != null) speedMul = scroller.SpeedMultiplier;

        // ─── ХАОС 1: периодически "ленюсь" ──────────────────────────
        bool isLazyNow = Time.time < _lazyUntil;
        if (Time.time > _nextLazyCheck)
        {
            _nextLazyCheck = Time.time + _lazyCheckPeriod;
            if (Random.value < _lazyChance)
                _lazyUntil = Time.time + _lazyDuration;
        }

        // ─── 1. Тянемся к юниту (с лёгким рандомом цели) ─────────────
        Unit nearest = _squad.GetNearestUnitByX(transform.position.x);

        // ХАОС 3: иногда (10% случаев) берём не ближайшего а случайного
        if (Random.value < 0.1f)
        {
            Unit randomUnit = _squad.GetRandomUnit();
            if (randomUnit != null) nearest = randomUnit;
        }

        float trackingDeltaX = 0f;

        if (nearest != null && !isLazyNow)
        {
            float targetX  = nearest.transform.position.x;
            float currentX = transform.position.x;
            float diff     = targetX - currentX;

            if (Mathf.Abs(diff) > 0.3f)
            {
                float pull = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(distZ / _trackingRange));
                // Применяем speedMul к tracking
                trackingDeltaX = Mathf.Sign(diff) * _trackingSpeed * _personalSpeedFactor * pull * speedMul * Time.deltaTime;
            }
        }

        // ─── ХАОС 2: покачивание по X (синус) ───────────────────────
        // Применяем speedMul и к покачиванию
        float wobble = Mathf.Sin(Time.time * _wobbleSpeed + _wobblePhase) * _wobbleAmount * speedMul * Time.deltaTime;

        // ─── 2. Отталкивание от других врагов ───────────────────────
        float separationDeltaX = 0f;
        float separationDeltaZ = 0f;

        foreach (EnemyMeleeAttack other in _allEnemies)
        {
            if (other == this || other._isDead) continue;

            Vector3 d = transform.position - other.transform.position;

            float distSqr = d.x * d.x + d.z * d.z;
            float radiusSqr = _separationRadius * _separationRadius;
            if (distSqr > radiusSqr) continue;

            float dist = Mathf.Sqrt(distSqr);
            if (dist < 0.01f) dist = 0.01f;

            // Применяем speedMul к разделению
            float strength = (1f - dist / _separationRadius) * _separationForce * speedMul * Time.deltaTime;
            separationDeltaX += (d.x / dist) * strength;
            separationDeltaZ += (d.z / dist) * strength;
        }

        // ─── 3. Применяем итоговое смещение ──────────────────────────
        float newX = transform.position.x + trackingDeltaX + separationDeltaX + wobble;
        float newZ = transform.position.z + separationDeltaZ;
        transform.position = new Vector3(newX, transform.position.y, newZ);
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

        // Через Enemy.TakeDamage с большим уроном — чтобы сработало событие OnAnyEnemyDied
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(99999, showDamageNumber: false); // тихая смерть от тарана
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
