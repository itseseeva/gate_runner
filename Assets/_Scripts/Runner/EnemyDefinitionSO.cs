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
}
