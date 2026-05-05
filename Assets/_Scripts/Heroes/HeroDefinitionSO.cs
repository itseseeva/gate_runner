using UnityEngine;

public enum HeroType { Warrior, Mage, Archer, Tank, Assassin, Healer, Support }

[CreateAssetMenu(fileName = "NewHero", menuName = "RPG/Hero Definition")]
public class HeroDefinitionSO : ScriptableObject
{
    [Header("Основное")]
    [SerializeField] private string _heroName  = "Новый герой";
    [SerializeField] private int    _maxHP     = 100;
    [SerializeField] private int    _attack    = 20;
    [SerializeField] private int    _defense   = 5;
    [SerializeField] private int    _baseSpeed = 100;
    [SerializeField] private int    _baseMana  = 50;

    [Header("Тип героя")]
    [SerializeField] private HeroType _heroType = HeroType.Warrior;

    [Header("Визуал и геймплей")]
    [Tooltip("Prefab T1 (базовая модель)")]
    [SerializeField] private GameObject _prefab;

    [Tooltip("Prefab T2 (после слияния 15 T1). Может быть null — тогда тип не апается.")]
    [SerializeField] private GameObject _prefabT2;

    [SerializeField] private float _attackRange = 5f;

    public string     HeroName    => _heroName;
    public int        MaxHP       => _maxHP;
    public int        Attack      => _attack;
    public int        Defense     => _defense;
    public int        BaseSpeed   => _baseSpeed;
    public int        BaseMana    => _baseMana;
    public HeroType   HeroType    => _heroType;
    public GameObject Prefab      => _prefab;
    public GameObject PrefabT2    => _prefabT2;
    public float      AttackRange => _attackRange;

    /// <summary>Возвращает prefab для нужного тира (T1 или T2).</summary>
    public GameObject GetPrefab(UnitTier tier)
    {
        return tier == UnitTier.T2 ? _prefabT2 : _prefab;
    }

    /// <summary>True если у этого типа есть T2-версия (можно апать).</summary>
    public bool CanUpgradeToT2 => _prefabT2 != null;

    /// <summary>
    /// УСТАРЕЛО: используется только до фазы 2.1 рефакторинга (CrowdManager заменит формацию).
    /// </summary>
    public static int GetFormationRow(HeroType type)
    {
        return type switch
        {
            HeroType.Tank     => 0,
            HeroType.Warrior  => 1,
            HeroType.Assassin => 1,
            HeroType.Mage     => 2,
            HeroType.Archer   => 2,
            HeroType.Healer   => 3,
            HeroType.Support  => 3,
            _                 => 2,
        };
    }
}
