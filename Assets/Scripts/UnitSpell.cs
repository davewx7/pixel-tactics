using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Wesnoth/UnitSpell")]
public class UnitSpell : GWScriptableObject
{
    public UnitMod modUntilEndOfTurn = null;

    public enum TargetType
    {
        Self,
        Ally,
        Enemy,
        Vacant,
    }

    static public List<UnitSpell> all {
        get {
            List<UnitSpell> result = new List<UnitSpell>();
            foreach(UnitSpell spell in AssetInfo.instance.allSpells) {
                if(spell != null) {
                    result.Add(spell);
                }
            }

            return result;
        }
    }


    public List<SpellSchool> schools;

    public bool ispotion {
        get {
            return consumable && targetTypes.Count == 1 && targetTypes[0] == TargetType.Self;
        }
    }
    public bool consumable = false;

    public int cooldown = -1;

    public List<TargetType> targetTypes = new List<TargetType>() { TargetType.Self, TargetType.Ally };
    public List<TerrainRules> targetTerrain = new List<TerrainRules>();
    public List<UnitAbility> targetHasAbility = new List<UnitAbility>();
    public bool targetLevelLessOrEqualToSpellLevel = false;
    public bool mustControlTarget = false;

    public bool targetsKeep = false;

    public int targetRange = 1;
    public int CasterRange(UnitInfo caster)
    {
        if(caster == null || targetRange == 0) {
            if(caster != null && caster.isAlchemist) {
                return 1;
            }
            return targetRange;
        }
        return targetRange + caster.spellRange;
    }

    public int targetRadius = 0;
    public bool rangeIsUnitVision = false;

    public bool rangeIsGlobal = false;

    public int healTarget = 0;
    public int temporaryHitpoints = 0;

    public int drainLife = 0;

    public bool blinkToTarget = false;

    public virtual bool usesAttack {
        get {
            return false;
        }
    }

    public string description;
    public Sprite icon;
    public float hueshift = 0f;

    public int spellLevel = 1;

    public bool refreshesMovement = false;
    public bool refreshesAttack = false;

    public string summaryRules;

    [AssetSelector(Paths = "Assets/GameScriptableObjects/UnitStatus")]
    public UnitStatus applyStatus;

    public UnitType summonType;
    public int numSummons = 1;

    public List<Equipment> createEquipment;

    public bool cannotTargetBlessedUnit = false;

    public SpecialEffect effectSource;

    public SpecialEffect effectDest;

    List<Equipment> CandidateEquipment(Loc loc)
    {
        List<Equipment> result = new List<Equipment>();

        Unit targetUnit = GameController.instance.GetUnitAtLoc(loc);
        if(targetUnit == null) {
            return result;
        }

        foreach(Equipment equip in createEquipment) {
            if(equip.EquippableForUnit(targetUnit.unitInfo)) {
                result.Add(equip);
            }
        }

        return result;
    }

    [TextArea(3, 5)]
    public string tooltipRules;

    public string GetTooltip(UnitInfo caster=null)
    {
        string rangeText = "";

        if(rangeIsUnitVision) {
            rangeText = "Unit vision";
            if(targetRadius > 0) {
                rangeText += ", Radius " + targetRadius;
            }
        }
        else if(targetRange == 0) {
            if(targetRadius == 0) {
                rangeText = "Self only";
            } else if(targetRadius == 1) {
                rangeText = "Self and adjacent";
            } else {
                rangeText = "Radius " + targetRadius + " around self";
            }
        } else {
            if(targetRadius == 0) {
                rangeText = targetRange.ToString();
            } else {
                string rangeStr = targetRange.ToString();
                int actualRange = CasterRange(caster);
                if(actualRange > targetRange) {
                    rangeStr = string.Format("<color=#aaffaa><b>{0}</b></color>", actualRange);
                }
                rangeText = string.Format("{0}, Radius {1}", rangeStr, targetRadius);
            }
        }

        string targetDescription = "";

        if(targetTypes.Count == 4) {
            targetDescription = "Any Tile";
        } else if(targetTypes.Count == 3 && targetTypes.Contains(TargetType.Vacant) == false) {
            targetDescription = "All Units";
        } else {
            foreach(var t in targetTypes) {
                string existing = targetDescription;
                switch(t) {
                    case TargetType.Ally:
                        targetDescription = "Allies";
                        break;
                    case TargetType.Enemy:
                        targetDescription = "Enemies";
                        break;
                    case TargetType.Self:
                        targetDescription = "Self";
                        break;
                    case TargetType.Vacant:
                        targetDescription = "Empty Tile";
                        break;
                }

                if(string.IsNullOrEmpty(existing) == false) {
                    targetDescription = string.Format("{0} or {1}", existing, targetDescription);
                }
            }
        }

        string cooldownText = "";
        if(consumable) {
            cooldownText = "<color=#aaaaaa>Consumable: This item is consumed when it is used.</color>\n";
        } else if(cooldown < 0) {
            cooldownText = "<color=#aaaaaa>Exhausts: After using this spell, must rest before using it again.</color>\n";
        } else if(cooldown <= 1) {
            cooldownText = "<color=#aaffaa>Cantrip: Does not go on cooldown after casting.</color>\n";
        } else {
            cooldownText = string.Format("<color=#aaffaa>Cooldown: {0} Moons. Rest to instantly reset the cooldown.</color>\n", cooldown);
        }

        return string.Format("<color=#ffffff>{0}:</color><color=#cccccc> {2}</color>\n{1}<color=#ffffff>Targets:</color> <color=#cccccc>{3}</color>\n<color=#ffffff>Range:</color><color=#cccccc> {4}</color>", description, cooldownText, tooltipRules, targetDescription, rangeText);
    }

    TargetType GetLocTargetType(Unit caster, Loc loc)
    {
        if(loc == caster.loc) {
            return TargetType.Self;
        }

        Unit targetUnit = GameController.instance.GetUnitAtLoc(loc);
        if(targetUnit == null) {
            return TargetType.Vacant;
        } else if(targetUnit.IsEnemy(caster)) {
            return TargetType.Enemy;
        } else {
            return TargetType.Ally;
        }
    }

    public int GetSpellSlotLevel(Unit caster)
    {
        int spellLevelSlot = spellLevel;
        for(int i = 0; i != caster.unitInfo.spells.Count; ++i) {
            if(caster.unitInfo.spells[i] == this) {
                int slotLevel = caster.unitInfo.unitType.unitInfo.spells[i].spellLevel;
                if(slotLevel > spellLevelSlot) {
                    spellLevelSlot = slotLevel;
                }
            }
        }

        return spellLevelSlot;
    }

    public bool IsValidTarget(Unit caster, Loc loc)
    {
        Tile tile = GameController.instance.map.GetTile(loc);
        if(targetTerrain.Count > 0 && tile != null && targetTerrain.Contains(tile.terrain.rules) == false) {
            return false;
        }

        if(mustControlTarget && GameController.instance.gameState.GetTeamOwnerOfLoc(loc) != caster.team) {
            return false;
        }

        if(createEquipment.Count > 0) {
            var equip = CandidateEquipment(loc);
            if(equip == null || equip.Count == 0) {
                return false;
            }
        }

        Unit targetUnit = GameController.instance.GetUnitAtLoc(loc);
        if(targetUnit != null) {
            foreach(var ability in targetHasAbility) {
                if(targetUnit.unitInfo.abilities.Contains(ability) == false) {
                    return false;
                }
            }
        }

        if(targetLevelLessOrEqualToSpellLevel && targetUnit != null && targetUnit.unitInfo.level > GetSpellSlotLevel(caster)) {
            return false;
        }

        if(targetUnit != null && cannotTargetBlessedUnit && targetUnit.unitInfo.blessed) {
            return false;
        }

        TargetType targetType = GetLocTargetType(caster, loc);
        if(targetType == TargetType.Ally && ispotion && caster.unitInfo.isAlchemist) {
            //alchemists can target allies with potions.
            return true;
        }
        return this.targetTypes.Contains(targetType);
    }

    [SerializeField]
    string _noTargetsErrorMessage = "No targets in range";

    public void StartCasting(Unit caster)
    {
        HashSet<Loc> targets = new HashSet<Loc>();
        List<Loc> locs = null;

        if(rangeIsGlobal) {
            locs = new List<Loc>();

            foreach(Loc loc in GameController.instance.map.dimensions.range) {
                locs.Add(loc);
                locs.Add(loc.toUnderworld);
            }
        }
        else if(rangeIsUnitVision) {
            locs = new List<Loc>();
            var paths = GameController.instance.CalculateUnitVision(caster);
            foreach(var p in paths) {
                locs.Add(p.Key);
            }
        } else if(targetsKeep) {
            locs = caster.teamInfo.keepsOwned;
        } else {
            locs = Tile.GetTilesInRadius(caster.loc, CasterRange(caster.unitInfo));
        }

        foreach(Loc loc in locs) {
            if(IsValidTarget(caster, loc)) {
                targets.Add(loc);
            }
        }

        if(targets.Count == 0) {
            GameController.instance.ShowUserErrorMessage(_noTargetsErrorMessage);
        } else {
            GameController.instance.SetCastingSpell(caster, this, targets);
        }
    }

    public void CompleteCasting(Unit caster, Loc target)
    {
        if(effectSource) {
            Tile tile = GameController.instance.map.GetTile(caster.loc);
            if(tile.fogged == false) {
                SpecialEffect effect = Instantiate(effectSource, tile.transform);
                effect.gameObject.SetActive(true);
            }
        }

        foreach(Loc loc in Tile.GetTilesInRadius(target, targetRadius)) {
            if(IsValidTarget(caster, loc)) {
                CompleteCastOnTarget(caster, loc);
            }
        }

        GameController.instance.RecalculateVision();
    }

    public void CompleteCastOnTarget(Unit caster, Loc target)
    {
        if(effectDest) {
            Tile tile = GameController.instance.map.GetTile(target);
            if(tile.fogged == false) {
                SpecialEffect effect = Instantiate(effectDest, tile.transform);
                effect.gameObject.SetActive(true);
            }
        }

        Unit targetUnit = GameController.instance.GetUnitAtLoc(target);
        if(healTarget > 0 && targetUnit != null) {
            targetUnit.Heal(healTarget);
        }

        if(targetUnit != null && createEquipment.Count > 0) {
            var equip = CandidateEquipment(target);
            if(equip != null && equip.Count > 0) {
                int index = GameController.instance.rng.Range(0, equip.Count);
                var item = equip[index];
                GameController.instance.ExecuteGrantEquipment(targetUnit, new List<Equipment>() { item });
            }
        }

        if(drainLife > 0 && targetUnit != null) {
            Unit drainSource = targetUnit;
            Unit drainDestination = caster;


            if(drainSource.team == drainDestination.team) {
                drainSource = caster;
                drainDestination = targetUnit;
            }

            int drainAmount = drainLife;
            if(drainSource.unitInfo.hitpointsRemaining <= drainAmount) {
                drainAmount = drainSource.unitInfo.hitpointsRemaining-1;
            }

            int temporaryHitpoints = 0;

            if(drainSource.team == drainDestination.team) {
                if(drainDestination.unitInfo.damageTaken < drainAmount) {
                    temporaryHitpoints = drainAmount - drainDestination.unitInfo.damageTaken;
                }
            }

            drainSource.unitInfo.damageTaken += drainAmount;
            drainDestination.unitInfo.damageTaken -= drainAmount - temporaryHitpoints;
            drainDestination.unitInfo.temporaryHitpoints += temporaryHitpoints;
        }

        if(applyStatus != null && targetUnit != null) {
            targetUnit.ApplyStatus(applyStatus);
        }

        if(temporaryHitpoints > 0 && targetUnit != null) {
            targetUnit.unitInfo.temporaryHitpoints += temporaryHitpoints;
        }

        if(refreshesAttack && targetUnit != null) {
            targetUnit.unitInfo.hasAttacked = false;
        }

        if(refreshesMovement && targetUnit != null) {
            targetUnit.unitInfo.movementExpended = 0;
        }


        if(blinkToTarget) {
            Loc sourceLoc = caster.loc;
            Unit unitAtTarget = GameController.instance.GetUnitAtLoc(target);
            if(unitAtTarget != null) {
                unitAtTarget.transform.position = GameController.instance.map.GetTile(sourceLoc).unitPos;
                unitAtTarget.loc = sourceLoc;
                GameController.instance.QueueUnitArriveAtDestination(unitAtTarget);
            }

            caster.transform.position = GameController.instance.map.GetTile(target).unitPos;
            caster.loc = target;
            GameController.instance.QueueUnitArriveAtDestination(caster);
        }

        if(summonType != null) {
            //remove existing familiars from the caster.
            caster.ClearFamiliars();

            for(int i = 0; i < numSummons; ++i) {

                Loc loc = GameController.instance.FindVacantTileNear(target);

                if(loc.valid) {
                    GameController.instance.ExecuteRecruit(new RecruitCommandInfo() {
                        unitType = summonType,
                        loc = loc,
                        summonerGuid = caster.unitInfo.guid,
                        isFamiliar = true,
                        unitStatus = new List<UnitStatus> { GameConfig.instance.statusTemporal },
                        haveHaste = true,
                    });
                }
            }
        }

        if(modUntilEndOfTurn.useMod && targetUnit != null) {
            targetUnit.unitInfo.AddModUntilEndOfTurn(modUntilEndOfTurn);
        }

        //If this is an actual spell from the unit's spell list.
        bool isSpell = caster.unitInfo.spells.Contains(this);
        if(isSpell) {
            //Trigger any equipment effects that occur anytime we cast a spell.
            foreach(Equipment equip in caster.unitInfo.equipment) {
                if(equip.castSpellEffect != null && equip.castSpellEffect.IsValidTarget(caster, target)) {
                    equip.castSpellEffect.CompleteCastOnTarget(caster, target);
                }
            }
        }
    }

}
