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
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float      _attackRange = 5f;

    public string     HeroName    => _heroName;
    public int        MaxHP       => _maxHP;
    public int        Attack      => _attack;
    public int        Defense     => _defense;
    public int        BaseSpeed   => _baseSpeed;
    public int        BaseMana    => _baseMana;
    public HeroType   HeroType    => _heroType;
    public GameObject Prefab      => _prefab;
    public float      AttackRange => _attackRange;

    /// <summary>
    /// Возвращает индекс ряда формации для этого типа героя.
    /// 0 = передний (Tank), 1 = середина (Warrior/Assassin),
    /// 2 = задний (Mage/Archer), 3 = тыл (Healer/Support).
    /// </summary>
    public static int GetFormationRow(HeroType type)
    {
        return type switch
        {
            HeroType.Tank      => 0, // Front
            HeroType.Warrior   => 1, // Mid
            HeroType.Assassin  => 1, // Mid
            HeroType.Mage      => 2, // Back
            HeroType.Archer    => 2, // Back
            HeroType.Healer    => 3, // Rear
            HeroType.Support   => 3, // Rear
            _                  => 2, // По умолчанию — back
        };
    }
}
