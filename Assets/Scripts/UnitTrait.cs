using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;


[System.Serializable]
public class AttackMod
{
    public List<string> attackDescription = new List<string>();
    public bool excludeMelee = false, excludedRanged = false, excludeMagical = false, excludePhysical = false;

    public int damage = 0;
    public int accuracy = 0;
    public int critical = 0;
    public int nstrikes = 0;

    [System.Serializable]
    public struct AbilityMod
    {
        [AssetsOnly]
        public AttackAbility ability;
        public int param;
        public bool useParam;
    }

    public List<AbilityMod> abilities = new List<AbilityMod>();

    public bool makesMagical = false;
    
    public bool AppliesToAttack(AttackInfo attack)
    {
        if(excludeMelee && attack.range == AttackInfo.Range.Melee) {
            return false;
        }

        if(excludedRanged && attack.range == AttackInfo.Range.Ranged) {
            return false;
        }

        if(excludePhysical && attack.attackType == AttackInfo.AttackType.Physical) {
            return false;
        }

        if(excludeMagical && attack.attackType == AttackInfo.AttackType.Magical) {
            return false;
        }

        if(attackDescription != null && attackDescription.Count != 0 && attackDescription.Contains(attack.description) == false) {
            return false;
        }

        return true;
    }

    public void Apply(UnitMod mod, ref AttackInfo attack, List<AttackModLog> log=null)
    {
        if(AppliesToAttack(attack) == false) {
            return;
        }

        attack.damage += damage;
        attack.accuracy += accuracy;
        attack.critical += critical;
        attack.nstrikes += nstrikes;

        if(log != null) {
            if(damage != 0) {
                log.Add(new AttackModLog() { description = mod.description, field = "DAM", delta = damage });
            }

            if(accuracy != 0) {
                log.Add(new AttackModLog() { description = mod.description, field = "ACC", delta = accuracy });
            }

            if(critical != 0) {
                log.Add(new AttackModLog() { description = mod.description, field = "CRIT", delta = critical });
            } 

            if(nstrikes != 0) {
                log.Add(new AttackModLog() { description = mod.description, field = "STRIKES", delta = nstrikes });
            }
        }

        foreach(AbilityMod m in abilities) {
            attack.AddAbility(m.ability);
            if(m.useParam) {
                attack.AddAbilityParam(m.ability, m.param);
            }
        }

        if(makesMagical) {
            attack.attackType = AttackInfo.AttackType.Magical;
        }
    }
}

[System.Serializable]
public struct ModFilter
{
    public UnitTag mustHaveTag;
    public bool excludeIfFoughtThisTurn;
    public UnitAbility baseAbility;
    public UnitStatus mustHaveStatus;

    public bool Passes(UnitInfo unitInfo)
    {
        if(excludeIfFoughtThisTurn && unitInfo.fightsThisTurn > 0) {
            return false;
        }
        if(mustHaveTag != null && unitInfo.unitType.tags.Contains(mustHaveTag) == false) {
            return false;
        }
        if(baseAbility != null && unitInfo.unitType.abilities.Contains(baseAbility) == false) {
            return false;
        }
        if(mustHaveStatus != null && unitInfo.status.Contains(mustHaveStatus) == false) {
            return false;
        }

        return true;
    }
}

[System.Serializable]
public class UnitMod
{
    public bool UseMod(UnitInfo unitInfo)
    {
        if(useMod == false) {
            return false;
        }

        return filter.Passes(unitInfo);
    }

    public string description = "(unknown)";

    public bool useMod = true;

    public ModFilter filter;

    public int hitpoints = 0;
    public int experience = 0;
    public int evasion = 0;
    public int limitEvasion = -1;
    public int criticalEvasion = 0;
    public int armor = 0;
    public int resistance = 0;
    public int movement = 0;
    public int spellRange = 0;

    public int vision = 0;

    public bool setAlignment = false;
    public UnitType.Alignment alignment;

    public List<AttackMod> attackMods = new List<AttackMod>();

    [AssetList(Path = "/GameScriptableObjects/UnitAbilities")]
    public List<UnitAbility> abilities = new List<UnitAbility>();

    public List<UnitAbilityArg> abilityArgs = new List<UnitAbilityArg>();

    int AdvantagedMod(int n)
    {
        if(n > 0)
            return n*2;
        else
            return 0;
    }

    int DisadvantagedMod(int n)
    {
        if(n > 0) {
            return 0;
        } else {
            return n*2;
        }
    }

    public UnitMod advantaged {
        get {
            UnitMod result = (UnitMod)MemberwiseClone();
            result.description += " (advantage)";
            result.hitpoints = AdvantagedMod(result.hitpoints);
            result.evasion = AdvantagedMod(result.evasion);
            result.criticalEvasion = AdvantagedMod(result.criticalEvasion);
            result.armor = AdvantagedMod(result.armor);
            result.resistance = AdvantagedMod(result.resistance);
            result.movement = AdvantagedMod(result.resistance);
            result.vision = AdvantagedMod(result.vision);

            return result;
        }
    }

    public UnitMod disadvantaged {
        get {
            UnitMod result = (UnitMod)MemberwiseClone();
            result.description += " (disadvantage)";
            result.hitpoints = DisadvantagedMod(result.hitpoints);
            result.evasion = DisadvantagedMod(result.evasion);
            result.criticalEvasion = DisadvantagedMod(result.criticalEvasion);
            result.armor = DisadvantagedMod(result.armor);
            result.resistance = DisadvantagedMod(result.resistance);
            result.movement = DisadvantagedMod(result.resistance);
            result.vision = DisadvantagedMod(result.vision);

            return result;
        }
    }

}

[CreateAssetMenu(menuName = "Wesnoth/Trait")]
public class UnitTrait : GWScriptableObject
{
    public string traitName;
    public string traitTooltip;

    public UnitMod unitMod = new UnitMod();
}
