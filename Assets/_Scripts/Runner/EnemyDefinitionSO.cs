using UnityEngine;

/// <summary>
/// Данные врага. Один файл SO на каждый тип врага.
/// Пример: Enemy_Basic_Data, Enemy_Elite_Data, Boss_Data.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_Data", menuName = "MGR/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Header("Основные параметры")]
    [SerializeField] private string _enemyName  = "Basic Enemy";
    [SerializeField] private int    _maxHP       = 50;

    [Header("Атака")]
    [Tooltip("Дистанция на которой враг встаёт и начинает бить")]
    [SerializeField] private float _attackRange  = 0.7f;

    [Tooltip("Атак в секунду")]
    [SerializeField] private float _attackSpeed  = 1f;

    [Tooltip("Урон за один удар")]
    [SerializeField] private int   _attackDamage = 10;

    [Header("Преследование")]
    [Tooltip("На какой дистанции сзади отряда преследовать (метры)")]
    [SerializeField, Range(0f, 20f)] private float _chaseDistance = 5f;

    [Tooltip("Хаос по X — разброс позиций (метры)")]
    [SerializeField, Range(0f, 3f)] private float _chaseChaosX = 1.5f;

    [Tooltip("Скорость движения в фазе Chase (м/сек). Меньше чем TrackingSpeed — движение читаемо.")]
    [SerializeField, Range(0.5f, 10f)] private float _chaseSpeed = 3f;

    [Header("Формация (расстояния)")]
    [Tooltip("На каком расстоянии враги отталкиваются друг от друга. Меньше = плотнее толпа.")]
    [SerializeField] private float _separationRadius       = 0.5f;

    [Tooltip("Минимальная дистанция до цели — насколько плотно враги прижимаются к герою")]
    [SerializeField] private float _separationTargetRadius = 0.4f;

    [Header("Спавн")]
    [Tooltip("Высота (Y), на которой враг появляется. Подбери так, чтобы снаряд попадал в тело.")]
    [SerializeField] private float _spawnHeight = 0.5f;

    [Header("Дальний бой")]
    [Tooltip("Префаб снаряда. Пусто = ближний бой.")]
    [SerializeField] private GameObject _projectilePrefab;

    [Tooltip("Кулдаун между выстрелами в секундах (только дальний бой)")]
    [SerializeField] private float _attackCooldown = 1.5f;

    [Tooltip("Высота спавна снаряда от позиции врага")]
    [SerializeField] private float _projectileSpawnHeight = 0.5f;

    // Дистанция стрельбы = AttackRange (используем общее поле).
    // Снаряд летит ровно на эту дистанцию + небольшой запас, чтобы точно долетел.

    // Публичный доступ — компоненты читают отсюда
    public string EnemyName             => _enemyName;
    public int    MaxHP                 => _maxHP;
    public float  AttackRange           => _attackRange;
    public float  AttackSpeed           => _attackSpeed;
    public int    AttackDamage          => _attackDamage;
    public float  SeparationRadius      => _separationRadius;
    public float  SeparationTargetRadius=> _separationTargetRadius;
    public float  SpawnHeight           => _spawnHeight;
    public float  ChaseDistance         => _chaseDistance;
    public float  ChaseChaosX           => _chaseChaosX;
    public float  ChaseSpeed            => _chaseSpeed;

    public GameObject ProjectilePrefab => _projectilePrefab;
    public bool IsRanged => _projectilePrefab != null;
    public float AttackCooldown => _attackCooldown;
    public float ProjectileSpawnHeight => _projectileSpawnHeight;

    [Header("Рывок (камикадзе)")]
    [Tooltip("Дистанция, с которой враг начинает рывок")]
    [SerializeField] private float _rollTriggerRange = 5f;

    [Tooltip("Скорость рывка (быстрее обычного движения)")]
    [SerializeField] private float _rollSpeed = 12f;

    [Tooltip("Радиус AoE-урона при столкновении")]
    [SerializeField] private float _rollAoeRadius = 2f;

    [Tooltip("Урон от рывка (обычно больше обычной атаки)")]
    [SerializeField] private int _rollDamage = 30;

    [Tooltip("Эффект взрыва при столкновении")]
    [SerializeField] private GameObject _rollExplosionEffect;

    public float      RollTriggerRange   => _rollTriggerRange;
    public float      RollSpeed          => _rollSpeed;
    public float      RollAoeRadius      => _rollAoeRadius;
    public int        RollDamage         => _rollDamage;
    public GameObject RollExplosionEffect => _rollExplosionEffect;

    public bool IsRoller => _rollExplosionEffect != null || _rollDamage > 0;
}
