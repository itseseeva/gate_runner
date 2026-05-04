using UnityEngine;

[CreateAssetMenu(fileName = "NewArtifact", 
                 menuName = "RPG/Artifact Definition")]
public class ArtifactDefinitionSO : ScriptableObject
{
    [Header("Основное")]
    public string artifactName = "Новый артефакт";
    public string description = "Описание артефакта";
    public Sprite icon;

    [Header("Редкость")]
    public ArtifactRarity rarity = ArtifactRarity.Common;

    [Header("Бонусы к статам")]
    public int bonusAttack = 0;
    public int bonusDefense = 0;
    public int bonusHP = 0;
}

public enum ArtifactRarity
{
    Common,     // Обычный
    Rare,       // Редкий
    Epic,       // Эпический
    Legendary   // Легендарный
}
