using System.Collections.Generic;

[System.Serializable]
public class HeroSaveData
{
    public string heroId;
    public int level;
    public int experience;
    public int statPoints;      // Очки для распределения по статам
    public int strength;
    public int intellect;
    public int agility;
    public int endurance;
    public List<string> equippedArtifactIds 
        = new List<string>();
}

[System.Serializable]
public class PlayerSaveData
{
    public int gold;
    public int crystals;
    public List<string> ownedArtifactIds 
        = new List<string>();
    public List<HeroSaveData> heroes 
        = new List<HeroSaveData>();
}
