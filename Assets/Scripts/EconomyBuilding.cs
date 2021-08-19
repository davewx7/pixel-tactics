using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Wesnoth/EconomyBuilding")]
public class EconomyBuilding : GWScriptableObject
{
    public string buildingName;
    public string tooltip;
    public string achievementText;

    [System.Serializable]
    public struct DialogInfo
    {
        public string title;
        public string message;
    }
    public List<DialogInfo> additionalAchievementText;

    public bool givesHaste = false;

    public Sprite icon;

    public Vector2Int loc;

    public int buildCost = 200;

    public List<UnitType> unitRecruits;
    public int recruitSlots = 0;

    public int increaseDiplomacy = 0;

    public int villageHealAmount = 0;

    public List<VillageBuilding> villageBuildingsAvailable;

    [System.Serializable]
    public struct SpellGrant
    {
        public List<UnitType> unitTypes;
        public List<UnitSpell> spells;
    }

    public List<SpellGrant> spells;

    //[AssetList(Path = "/GameScriptableObjects/Equipment")]
    public List<Equipment> equipmentInMarket = new List<Equipment>();

    public int recruitExperienceBonus = 0;
    public bool potionGranted = false;

    public Equipment grantsEquipmentOnBuild = null;

    public int modMaxUnits = -1;
    public UnitMod globalUnitMod;

    public bool doubleXpHighLevelUnits = false;

    public int temporalUnitDuration = 0;
}
