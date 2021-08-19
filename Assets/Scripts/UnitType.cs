using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public struct AttackAbilityParam
{
    public AttackAbility ability;
    public int paramValue;
}

public struct AttackModLog
{
    public string description;
    public string field;
    public int delta;
}

[System.Serializable]
public struct AttackInfo
{
    [AssetList(Path ="/UITextures/Icons/attacks")]
    [PreviewField(70, ObjectFieldAlignment.Center)]
    public Sprite icon;

    public string description;
    public string id;
    public enum AttackType { Physical, Magical };
    public enum Range { Melee, Ranged };
    public int damage;
    public int nstrikes;
    public int accuracy;
    public int critical;
    public int criticalDamage;
    public Range range;
    public AttackType attackType;

    [System.NonSerialized]
    public List<AttackModLog> modLog;

    public AttackAbility[] abilities;
    public AttackAbilityParam[] abilityParams;

    //description of active effects in this attack.
    public string activeEffects;

    public string attackEffectsDescription {
        get {
            string result = "";
            if(lifestealPercent > 0) {
                result += string.Format("{0}% lifesteal ", lifestealPercent);
            }

            foreach(UnitStatus status in applyStatus) {
                result += status.description + " ";
            }

            result += activeEffects;

            return result;
        }
    }

    public int GetAbilityParam(AttackAbility ability)
    {
        if(HasAbility(ability) == false) {
            return 0;
        }

        for(int i = 0; i < abilityParams.Length; ++i) {
            if(abilityParams[i].ability == ability) {
                return abilityParams[i].paramValue;
            }
        }

        return ability.defaultParam;
    }

    public void AddAbility(AttackAbility ability)
    {
        if(abilities == null || abilities.Length == 1) {
            abilities = new AttackAbility[1];
            abilities[0] = ability;
        }

        if(HasAbility(ability)) {
            return;
        }

        AttackAbility[] newAbilities = new AttackAbility[abilities.Length+1];
        newAbilities[0] = ability;
        for(int i = 0; i != abilities.Length; ++i) {
            newAbilities[i+1] = abilities[i];
        }

        abilities = newAbilities;
    }

    public void AddAbilityParam(AttackAbility ability, int val)
    {
        if(abilityParams == null || abilityParams.Length == 0) {
            abilityParams = new AttackAbilityParam[1];
            abilityParams[0].ability = ability;
            abilityParams[0].paramValue = val;
            return;
        }

        for(int i = 0; i < abilityParams.Length; ++i) {
            if(abilityParams[i].ability == ability) {
                abilityParams[i].paramValue += val;
                return;
            }
        }

        var newParams = new AttackAbilityParam[abilityParams.Length+1];
        newParams[0].ability = ability;
        newParams[0].paramValue = val;

        for(int i = 0; i < abilityParams.Length; ++i) {
            newParams[i+1] = abilityParams[i];
        }

        abilityParams = newParams;
    }

    public int numCharges;

    public bool isAssassinate {
        get {
            return HasAbility(GameConfig.instance.attackAbilityAssassinate);
        }
    }

    public bool isBackstab {
        get {
            return HasAbility(GameConfig.instance.attackAbilityBackstab);
        }
    }

    public bool isBludgeoning {
        get {
            return HasAbility(GameConfig.instance.attackAbilityBludgeon);
        }
    }

    public bool isCharge {
        get {
            return HasAbility(GameConfig.instance.attackAbilityCharge);
        }
    }


    public bool isPoison {
        get {
            return HasAbility(GameConfig.instance.attackAbilityPoison);
        }
    }

    public bool isPolearm {
        get {
            return HasAbility(GameConfig.instance.attackAbilityPolearm);
        }
    }

    public bool isFirstStrike {
        get {
            return HasAbility(GameConfig.instance.attackAbilityFirstStrike);
        }
    }

    public bool isBerserk {
        get {
            return HasAbility(GameConfig.instance.attackAbilityBerserk);
        }
    }

    public bool isZombify {
        get {
            return HasAbility(GameConfig.instance.attackAbilityZombify);
        }
    }

    public int lifestealPercent {
        get {
            return GetAbilityParam(GameConfig.instance.attackAbilityLifesteal);
        }
    }

    public bool HasAbility(AttackAbility ability)
    {
        foreach(AttackAbility a in abilities) {
            if(a == ability) {
                return true;
            }
        }

        return false;
    }

    public List<UnitStatus> applyStatus {
        get {
            List<UnitStatus> result = new List<UnitStatus>();
            foreach(AttackAbility a in abilities) {
                if(a.applyStatus != null) {
                    result.Add(a.applyStatus);
                }
            }

            return result;
        }
    }

    [AssetList(Path = "/GameScriptableObjects/Projectiles")]
    public ProjectileType projectileType;

    public string rangeDescription { get { return range == Range.Melee ? "melee" : "ranged"; } }
}

[CreateAssetMenu(menuName = "Wesnoth/UnitType")]
public class UnitType : GWScriptableObject
{
    public static List<UnitType> GetAll()
    {
        List<UnitType> result = new List<UnitType>();

        foreach(var p in GWScriptableObject.allObjects) {
            UnitType u = p.Value as UnitType;
            if(u != null) {
                result.Add(u);
            }
        }

        return result;
    }

    public Sprite portrait;
    public Sprite portraitFemale;

    public string description;

    public string classId;
    public string raceId;

    public string classDescription { get { return classId; } }
    public string raceDescription { get { return raceId; } }

    public List<UnitTag> tags = new List<UnitTag>();

    public List<UnitAbility> abilities = new List<UnitAbility>();
    public List<UnitAbilityArg> abilityArgs = new List<UnitAbilityArg>();

    public int cost = 10;

    public enum Alignment { Lawful, Neutral, Chaotic };
    public Alignment alignment = Alignment.Neutral;

    public List<SpellSchool> spellSchoolsOverride = new List<SpellSchool>();

    public UnitInfo unitInfo {
        get {
            _unitInfo.unitType = this;
            return _unitInfo;
        }
    }

    public int AIThinkOrder = 0;

    [SerializeField]
    UnitInfo _unitInfo = new UnitInfo();

    public UnitInfo createUnit(int seed=-1, TeamInfo teamInfo=null)
    {
        UnitInfo result = unitInfo.Clone();
        result.seed = seed;

        ConsistentRandom rng = new ConsistentRandom(seed);

        result.roundCreated = GameController.instance.gameState.nround;
        result.characterName = "";
        foreach(UnitTag tag in tags) {
            if(tag.hasNames) {
                result.characterName = tag.GenerateName(rng, result.gender);
                break;
            }
        }

        List<UnitTrait> possibleTraits = new List<UnitTrait>();

        if(seed != -1) {
            int ntraits = 0;
            foreach(UnitTag tag in tags) {
                ntraits += tag.numberOfTraits;

                foreach(UnitTrait t in tag.traits) {
                    if(possibleTraits.Contains(t) == false) {
                        possibleTraits.Add(t);
                    }
                }
            }

            while(ntraits < possibleTraits.Count) {
                int index = rng.Range(0, possibleTraits.Count);
                possibleTraits.RemoveAt(index);
            }

            result.traits = possibleTraits;
        }

        if(teamInfo != null) {
            result.experience += teamInfo.recruitExperienceBonus;

            if(teamInfo.unitsGrantedItemsOnRecruit && teamInfo.consumablesInMarket.Count > 0) {
                int index = rng.Next(teamInfo.consumablesInMarket.Count);
                result.equipment.Add(teamInfo.consumablesInMarket[index]);
            }

            if(teamInfo.unitsGrantedItemsOnRecruit && teamInfo.equipmentInMarket.Count > 0) {
                int index = rng.Next(teamInfo.equipmentInMarket.Count);
                result.equipment.Add(teamInfo.equipmentInMarket[index]);
            }

        }

        return result;
    }

    public int experienceMax = 30;
    public int hitpointsMax = 30;

    public int criticalEvasion = 0;
    public int evasion = 0;
    public int armor = 0;
    public int resistance = 0;

    public int movement = 5;

    public List<UnitType> levelsInto = new List<UnitType>();

    public List<AttackInfo> attacks = new List<AttackInfo>();

    public List<UnitGender> possibleGenders = new List<UnitGender>();

    public List<AnimInfo> animInfo = new List<AnimInfo>();

    public AnimInfo GetAnim(AnimMatch query)
    {
        AnimInfo result = null;
        int bestScore = 0;
        int bestIndex = -1;
        int nindex = 0;
        foreach(AnimInfo a in animInfo) {
            int score = a.matchInfo.MatchQuality(query);
            if(score > bestScore) {
                bestScore = score;
                result = a;
                bestIndex = nindex;
            }

            ++nindex;
        }

        return result;
    }

    public Sprite avatarImage { get { return animInfo[0].sprites[0]; } }

    void Awake()
    {
    }

}
