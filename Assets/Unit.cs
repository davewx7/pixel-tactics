using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Profiling;
using Sirenix;

[System.Serializable]
public class UnitInfo
{
    public UnitInfo Clone()
    {
        var result = (UnitInfo)MemberwiseClone();
        result.characterMods = new List<UnitMod>(characterMods);
        result.equipment = new List<Equipment>(equipment);
        result.traits = new List<UnitTrait>(traits);
        result.spells = new List<UnitSpell>(spells);
        result._spellsCast = new List<UnitInfo.SpellCastInfo>(_spellsCast);
        result.status = new List<UnitStatus>(status);
        result.statusRemoveEndOfTurn = new List<UnitStatus>(statusRemoveEndOfTurn);
        result.attacksExpended = new List<string>(attacksExpended);
        result._modUntilEndOfTurn = new List<UnitMod>(_modUntilEndOfTurn);
        result.guid = System.Guid.NewGuid().ToString();

        if(this.killedByPlayerEvent != null && this.killedByPlayerEvent.valid) {
            result.killedByPlayerEvent = this.killedByPlayerEvent;
        } else {
            result.killedByPlayerEvent = null;
        }

        return result;
    }

    public UnitInfo RefreshFromUnitType(UnitType unitType, bool resetSpells=false)
    {
        //Refreshing a unit from basic type information means making a clone of the
        //unit info from the type, but keeping our identity through our guid, seed,
        //character name, character mods, team, and keeping our items, equipment, traits, and spells.
        UnitInfo info = unitType.unitInfo.Clone();

        info.guid = this.guid;
        info.seed = this.seed;
        info.characterName = this.characterName;
        info.characterMods = new List<UnitMod>(this.characterMods);
        info.ruler = this.ruler;
        info.loc = this.loc;
        info.nteam = this.nteam;
        info.experience = this.experience;
        info.equipment = new List<Equipment>(this.equipment);
        info.traits = new List<UnitTrait>(this.traits);
        info.attacksExpended = new List<string>();
        info.amla = this.amla;

        if(resetSpells == false) {
            info.spells = new List<UnitSpell>(this.spells);
        }
        info._spellsCast = new List<UnitInfo.SpellCastInfo>();

        if(this.killedByPlayerEvent != null && this.killedByPlayerEvent.valid) {
            info.killedByPlayerEvent = this.killedByPlayerEvent;
        } else {
            info.killedByPlayerEvent = null;
        }

        if(isUnhealable) {
            info.damageTaken = this.damageTaken;
        }

        return info;
    }

    public int fightsThisTurn = 0;

    public void BeginTurn()
    {
        fightsThisTurn = 0;
        temporaryHitpoints = 0;
        spellsChangedThisTurn = false;

        if(this.abilities.Contains(GameConfig.instance.unitAbilityShielded)) {
            temporaryHitpoints = this.GetAbilityArg(GameConfig.instance.unitAbilityShielded);
        }

        if(status.Contains(GameConfig.instance.statusFallingAsleep)) {
            status.Remove(GameConfig.instance.statusFallingAsleep);
            status.Add(GameConfig.instance.statusSleep);
        }

        if(sleeping == false) {
            hasAttacked = false;
            tired = false;
            movementExpended = 0;
            expendedVision = false;
        }

        ApplyPoisonAndRegeneration();

        _modUntilEndOfTurn = new List<UnitMod>();
    }

    public void EndTurn()
    {
        VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);
        if(building != null) {
            if(building.regenerateAtEndOfTurn && regenerationAmount > 0) {
                ApplyPoisonAndRegeneration();
            }

            if(building.temporaryHitpointsEndOfTurn > 0) {
                temporaryHitpoints += building.temporaryHitpointsEndOfTurn;
            }
        }
    }

    public void ApplyPoisonAndRegeneration()
    {
        if(GameController.instance.spectating) {
            return;
        }

        if(poisoned && hitpointsRemaining > 1) {
            int damage = 8;
            if(damage >= hitpointsRemaining) {
                damage = hitpointsRemaining-1;
            }

            List<UnitStatus> removeStatus = new List<UnitStatus>();
            if(regenerationAmount > 0) {
                damage = 0;
                removeStatus.Add(GameConfig.instance.statusPoisoned);
            }

            GameController.instance.ScrollCameraTo(loc);
            GameController.instance.QueueBeginTurnEffect(new BeginTurnEffectInfo() {
                unitGuid = this.guid,
                healing = -damage,
                removeStatus = removeStatus,
            });
        } else if(regenerationAmount > 0 && damageTaken > 0 && isUnhealable == false) {
            int heal = regenerationAmount;

            if(heal >= damageTaken) {
                heal = damageTaken;
            }

            GameController.instance.ScrollCameraTo(loc);
            GameController.instance.QueueBeginTurnEffect(new BeginTurnEffectInfo() {
                unitGuid = this.guid,
                healing = heal,
            });
        }
    }

    public UnitGender gender {
        get {
            if(unitType.possibleGenders.Count == 0) {
                return UnitGender.None;
            } else if(unitType.possibleGenders.Count == 1) {
                return unitType.possibleGenders[0];
            }

            return unitType.possibleGenders[seed%unitType.possibleGenders.Count];
        }
    }

    public string guid = System.Guid.NewGuid().ToString();

    public string characterName = "CharName";

    //Mods for a unique character who is a little different to other
    //units of this type.
    public List<UnitMod> characterMods = new List<UnitMod>();

    public Sprite portrait {
        get {
            if(gender == UnitGender.Female && unitType.portraitFemale != null) {
                return unitType.portraitFemale;
            }

            return unitType.portrait;
        }
    }

    public bool ruler = false;

    [SerializeField]
    public Tile.Direction facing = Tile.Direction.South;


    public int level = 1;
    public int nteam = 0;

    public int ncontroller {
        get {
            return nteam;
        }
    }

    public int seed = 0;
    public UnitType unitType;
    public Loc loc;

    public int roundCreated = 0;

    [HideInInspector]
    public string summonerGuid = "";

    public string zombietype = "";

    [HideInInspector]
    public List<string> familiarGuids = new List<string>();

    public void ExpendAttackCharge(string attackid)
    {
        attacksExpended.Add(attackid);
    }

    public List<string> attacksExpended = new List<string>();
    public int GetAttacksExpended(string attackId)
    {
        int result = 0;
        foreach(string s in attacksExpended) {
            if(s == attackId) {
                ++result;
            }
        }

        return result;
    }

    public List<Equipment> equipment = new List<Equipment>();

    public int numInventorySlots {
        get {
            return 4;
        }
    }

    public int numInventorySlotsUsed {
        get {
            return equipment.Count;
        }
    }

    //The side that this unit 'originated' with if they have gone to serve another side.
    public Team originTeam = null;

    public List<UnitTrait> traits = new List<UnitTrait>();

    public List<UnitStatus> status = new List<UnitStatus>();

    //Many status items follow the 'one round' rule for when they are removed.
    //This means that if the unit had the status inflicted during its turn,
    //it ends at the beginning of the next turn. If the unit had the status
    //inflicted on an enemy's turn, it ends at the end of the unit's next turn.
    //
    //This means that at begin turn and end turn any statuses appearing in this
    //list are removed. Any statuses not in this list are added to the list.
    public List<UnitStatus> statusRemoveEndOfTurn = new List<UnitStatus>();

    public void StatusTurnBoundary()
    {
        if(status.Count == 0) {
            statusRemoveEndOfTurn.Clear();
            return;
        }

        foreach(UnitStatus statusItem in statusRemoveEndOfTurn) {
            status.Remove(statusItem);
        }

        statusRemoveEndOfTurn.Clear();

//        statusRemoveEndOfTurn.Clear();

        foreach(UnitStatus statusItem in status) {
            if(statusItem.expiresAfterOneRound) {
                statusRemoveEndOfTurn.Add(statusItem);
            }
        }
    }

    public bool charmed {
        get {
            return status.Contains(GameConfig.instance.statusCharmed);
        }
    }

    public bool sleeping {
        get {
            return status.Contains(GameConfig.instance.statusSleep) || status.Contains(GameConfig.instance.statusFallingAsleep);
        }
    }

    public bool levitating {
        get {
            return status.Contains(GameConfig.instance.statusLevitating);
        }
    }

    public bool poisoned {
        get {
            return status.Contains(GameConfig.instance.statusPoisoned);
        }
    }

    public bool blessed {
        get {
            return status.Contains(GameConfig.instance.statusBlessingOfStrength) || status.Contains(GameConfig.instance.statusBlessingOfProtection);
        }
    }

    public bool cursed {
        get {
            return status.Contains(GameConfig.instance.statusCurse);
        }
        set {
            if(value != cursed) {
                if(value) {
                    status.Add(GameConfig.instance.statusCurse);
                } else {
                    status.Remove(GameConfig.instance.statusCurse);
                }
            }
        }
    }


    public string alignmentDescription {
        get {
            var align = alignment;
            if(align == UnitType.Alignment.Lawful) {
                return "Lawful";
            } else if(align == UnitType.Alignment.Chaotic) {
                return "Chaotic";
            } else {
                return "Neutral";
            }
        }
    }

    public UnitType.Alignment alignment {
        get {
            var mods = _modsExcludingTerrain;
            var result = unitType.alignment;
            foreach(var m in mods) {
                if(m.setAlignment) {
                    result = m.alignment;
                }
            }

            return result;
        }
    }

    public List<UnitMod> permanentMods {
        get {
            var result = new List<UnitMod>(characterMods);
            foreach(var trait in traits) {
                if(trait.unitMod.UseMod(this)) {
                    result.Add(trait.unitMod);
                }
            }

            foreach(var equipment in equipment) {
                if(equipment.mod.UseMod(this)) {
                    result.Add(equipment.mod);
                }
            }

            var teamInfo = GameController.instance.teams[nteam];
            foreach(var economyBuilding in teamInfo.buildingsCompleted) {
                if(economyBuilding.modMaxUnits > 0 && teamInfo.numUnits <= economyBuilding.modMaxUnits) {
                    result.Add(economyBuilding.globalUnitMod);
                }
            }

            for(int i = 0; i < amla; ++i) {
                result.Add(GameConfig.instance.amlaMod);
            }

            return result;
        }
    }

    List<UnitMod> _modsExcludingTerrainAndAbilities {
        get {
            var result = permanentMods;

            foreach(var statusItem in this.status) {
                if(statusItem.unitMod.UseMod(this)) {
                    result.Add(statusItem.unitMod);
                }
            }

            foreach(var turnMod in _modUntilEndOfTurn) {
                if(turnMod.UseMod(this)) {
                    result.Add(turnMod);
                }
            }

            //Don't query alignment here but calculate it from the mods.
            var align = unitType.alignment;
            foreach(var mod in result) {
                if(mod.setAlignment) {
                    align = mod.alignment;
                }
            }

            var seasonMod = GameController.instance.currentMonth.season.GetAlignmentMod(align);
            if(seasonMod != null && seasonMod.UseMod(this)) {
                result.Add(seasonMod);
            }

            VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);
            if(building != null && building.unitMod.useMod) {
                result.Add(building.unitMod);
            }

            return result;
        }
    }


    List<UnitMod> _modsExcludingTerrain {
        get {
            var result = _modsExcludingTerrainAndAbilities;
            var abilities = CalculateAbilities(result);

            foreach(var ability in abilities) {
                if(ability.unitMod.UseMod(this)) {
                    result.Add(ability.unitMod);
                }
            }

            return result;
        }
    }

    public UnitMod GetModForTerrain(TerrainRules t)
    {
        if(AdvantageInTerrain(t)) {
            return t.unitMod.advantaged;
        } else if(DisadvantageInTerrain(t)) {
            return t.unitMod.disadvantaged;
        } else {
            return t.unitMod;
        }
    }

    List<UnitMod> _modsCache = null;
    int _modsCacheState = -1;

    public List<UnitMod> mods {
        get {
            if(_modsCache != null && _modsCacheState == GameController.instance.gameState.stateid) {
                return _modsCache;
            }

            var result = _modsExcludingTerrain;
            if(isFlying == false) {
                var mod = GetModForTerrain(GameController.instance.map.GetTile(loc).terrain.rules);
                if(mod.UseMod(this)) {
                    result.Add(mod);
                }
            }

            _modsCache = result;
            _modsCacheState = GameController.instance.gameState.stateid;

            return result;
        }
    }


    [SerializeField]
    List<UnitMod> _modUntilEndOfTurn = new List<UnitMod>();

    public void AddModUntilEndOfTurn(UnitMod mod)
    {
        _modUntilEndOfTurn.Add(mod);
    }

    List<UnitAbility> CalculateAbilities(List<UnitMod> unitMods, List<string> sourceDescriptions=null, List<UnitAbilityArg> args=null)
    {
        List<UnitAbility> result = new List<UnitAbility>(unitType.abilities);
        if(sourceDescriptions != null) {
            while(sourceDescriptions.Count < result.Count) {
                sourceDescriptions.Add("innate");
            }
        }

        if(args != null) {
            foreach(UnitAbilityArg arg in unitType.abilityArgs) {
                args.Add(arg);
            }
        }

        foreach(var mod in unitMods) {
            foreach(var ability in mod.abilities) {
                result.Add(ability);

                if(sourceDescriptions != null) {
                    sourceDescriptions.Add(mod.description);
                }
            }

            if(args != null) {
                foreach(var abilityArg in mod.abilityArgs) {
                    bool found = false;
                    for(int i = 0; i != args.Count; ++i) {
                        if(args[i].ability == abilityArg.ability) {
                            var info = args[i];
                            info.arg += abilityArg.arg;
                            args[i] = info;
                            found = true;
                        }
                    }

                    if(found == false) {
                        args.Add(abilityArg);
                    }
                }
            }
        }

        //get the terrain mod here and count it.
        var terrainMod = GetModForTerrain(GameController.instance.map.GetTile(loc).terrain.rules);
        foreach(var ability in terrainMod.abilities) {
            result.Add(ability);

            if(sourceDescriptions != null) {
                sourceDescriptions.Add("terrain");
            }
        }

        return result;
    }

    public List<UnitAbilityArg> abilityArgs {
        get {
            List<UnitAbilityArg> result = new List<UnitAbilityArg>();
            CalculateAbilities(_modsExcludingTerrainAndAbilities, null, result);
            return result;
        }
    }

    public int GetAbilityArg(UnitAbility ability)
    {
        foreach(var arg in abilityArgs) {
            if(arg.ability == ability) {
                return arg.arg;
            }
        }

        return 0;
    }

    static ProfilerMarker s_profileAbilities = new ProfilerMarker("Unit.abilities");

    List<UnitAbility> _abilitiesCache = null;
    int _abilitiesCacheState = -1;

    public List<UnitAbility> abilities {
        get {
            if(_abilitiesCache != null && _abilitiesCacheState == GameController.instance.gameState.stateid) {
                return _abilitiesCache;
            }

            using(s_profileAbilities.Auto()) {
                _abilitiesCache = CalculateAbilities(_modsExcludingTerrainAndAbilities);
                _abilitiesCacheState = GameController.instance.gameState.stateid;
                return _abilitiesCache;
            }
        }
    }

    public List<string> abilitySourceDescriptions {
        get {
            List<string> result = new List<string>();
            CalculateAbilities(_modsExcludingTerrainAndAbilities, result);
            return result;
        }
    }

    public bool isAlchemist {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityAlchemist);
        }
    }

    public bool isAquatic {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityAquatic);
        }
    }

    public bool isCavalry {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityCavalry);
        }
    }

    public bool isEthereal {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityEthereal);
        }
    }

    public bool isFlanker {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityFlank);
        }
    }

    static ProfilerMarker s_profileIsFlying = new ProfilerMarker("Unit.isFlying");


    public bool isFlying {
        get {
            using(s_profileIsFlying.Auto()) {
                return abilities.Contains(GameConfig.instance.unitAbilityFlying);
            }
        }
    }

    public bool isInvisible {
        get {
            if(GameController.instance.playerInvisible && ncontroller == 0)
                return true;
            return abilities.Contains(GameConfig.instance.unitAbilityInvisible);
        }
    }

    public bool isUndead {
        get {
            return unitType.tags.Contains(GameConfig.instance.undeadTag);
        }
    }

    //the kind of trait a zombie version of this unit would have.
    public UnitTrait zombieTrait {
        get {
            if(isCavalry) {
                return GameConfig.instance.mountedZombieUnitTrait;
            }

            foreach(var tag in unitType.tags) {
                if(tag.zombieTrait != null) {
                    return tag.zombieTrait;
                }
            }

            return null;
        }
    }

    public int regenerationAmount {
        get {
            if(abilities.Contains(GameConfig.instance.unitAbilityRegeneration)) {
                return 8;
            }

            Tile t = GameController.instance.map.GetTile(loc);
            if(t.terrain.rules.keep) {
                return 8;
            } else if(t.terrain.rules.village) {
                return GameController.instance.teams[nteam].villageHealAmount;
            }

            return 0;
        }
    }

    public bool isTemporal {
        get {
            return status.Contains(GameConfig.instance.statusTemporal);
        }
    }


    public bool isShieldWall {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityShieldWall);
        }
    }

    public bool isSkirmish {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilitySkirmish);
        }
    }

    public bool isUnhealable {
        get {
            return abilities.Contains(GameConfig.instance.unitAbilityUnhealable);
        }
    }

    public int criticalEvasion {
        get {
            int result = unitType.criticalEvasion;
            foreach(var mod in mods) {
                result += mod.criticalEvasion;
            }
            return result;
        }
    }

    public string evasionCalc {
        get {
            string result = string.Format("innate: {0}%", unitType.evasion);
            List<UnitMod> unitMods = mods;
            foreach(var mod in unitMods) {
                if(mod.evasion != 0) {
                    result += string.Format("\n{0}: {1}{2}%", mod.description, mod.evasion > 0 ? "+" : "", mod.evasion);
                }
            }

            foreach(var mod in unitMods) {
                if(mod.limitEvasion != -1) {
                    result += string.Format("\n{0}: limit to {1}%", mod.description, mod.limitEvasion);
                }
            }

            result += string.Format("\ntotal: {0}%", evasion);
            return result;
        }
    }

    int CalcEvasionWithMods(List<UnitMod> unitMods)
    {
        int result = unitType.evasion;
        foreach(var mod in unitMods) {
            result += mod.evasion;
        }
        foreach(var mod in unitMods) {
            if(mod.limitEvasion != -1 && result > mod.limitEvasion) {
                result = mod.limitEvasion;
            }
        }

        return result;
    }

    public int evasion {
        get {
            return CalcEvasionWithMods(mods);
        }
    }

    public int evasionPermanent {
        get {
            return CalcEvasionWithMods(permanentMods);
        }
    }


    public string armorCalc {
        get {
            string result = string.Format("innate: {0}", unitType.armor);
            foreach(var mod in mods) {
                if(mod.armor != 0) {
                    result += string.Format("\n{0}: {1}{2}", mod.description, mod.armor > 0 ? "+" : "", mod.armor);
                }
            }

            result += string.Format("\ntotal: {0}", armor);
            return result;
        }
    }


    public int armor {
        get {
            int result = unitType.armor;
            foreach(var mod in mods) {
                result += mod.armor;
            }
            return result;
        }
    }

    public int armorPermanent {
        get {
            int result = unitType.armor;
            foreach(var mod in permanentMods) {
                result += mod.armor;
            }
            return result;
        }
    }


    public string resistanceCalc {
        get {
            string result = string.Format("innate: {0}", unitType.resistance);
            foreach(var mod in mods) {
                if(mod.resistance != 0) {
                    result += string.Format("\n{0}: {1}{2}", mod.description, mod.resistance > 0 ? "+" : "", mod.resistance);
                }
            }

            result += string.Format("\ntotal: {0}", resistance);
            return result;
        }
    }

    public int resistance {
        get {
            int result = unitType.resistance;
            foreach(var mod in mods) {
                result += mod.resistance;
            }
            return result;
        }
    }

    public int resistancePermanent {
        get {
            int result = unitType.resistance;
            foreach(var mod in permanentMods) {
                result += mod.resistance;
            }
            return result;
        }
    }


    public bool hasAdjacentAllies {
        get {
            foreach(Tile adj in GameController.instance.map.GetTile(loc).adjacentTiles) {
                if(adj.unit != null && adj.unit.unitInfo.IsAlly(this)) {
                    return true;
                }
            }

            return false;
        }
    }

    public bool CannotEnterTerrain(TerrainRules terrain)
    {
        foreach(var tag in unitType.tags) {
            if(tag.cannotEnterTerrain.Contains(terrain)) {
                return true;
            }
        }

        return false;
    }

    public bool AdvantageInTerrain(TerrainRules terrain)
    {
        foreach(var tag in unitType.tags) {
            if(tag.advantagedTerrain.Contains(terrain)) {
                return true;
            }
        }

        foreach(var equip in equipment) {
            if(equip.advantagedTerrain.Contains(terrain)) {
                return true;
            }
        }
        return false;
    }

    public bool DisadvantageInTerrain(TerrainRules terrain)
    {
        foreach(var tag in unitType.tags) {
            if(tag.disadvantagedTerrain.Contains(terrain)) {
                return true;
            }
        }

        foreach(UnitAbility ability in unitType.abilities) {
            if(ability.disadvantageInTerrain.Contains(terrain)) {
                return true;
            }
        }

        foreach(var equip in equipment) {
            if(equip.disadvantagedTerrain.Contains(terrain)) {
                return true;
            }
        }

        return false;
    }

    public void ApplyDefense(UnitInfo attacker, ref AttackInfo attack)
    {
        attack.critical -= criticalEvasion;
        attack.accuracy -= evasion;

        if(criticalEvasion != 0) {
            if(attack.modLog != null) {
                attack.modLog?.Add(new AttackModLog() {
                    description = "Enemy's crit. evasion",
                    field = "CRIT",
                    delta = -criticalEvasion,
                });
            }
        }

        if(attack.modLog != null) {
            attack.modLog?.Add(new AttackModLog() {
                description = "Enemy's evasion",
                field = "ACC",
                delta = -evasion,
            });
        }

        if(this.isFlying && attacker.isFlying == false && attack.range == AttackInfo.Range.Melee) {
            attack.accuracy -= 30;

            if(attack.modLog != null) {
                attack.modLog?.Add(new AttackModLog() {
                    description = "flying",
                    field = "ACC",
                    delta = -30,
                });
            }
        }

        if(attack.attackType == AttackInfo.AttackType.Physical) {
            if(attack.isBludgeoning == false) {

                attack.damage -= armor;
                attack.modLog?.Add(new AttackModLog() {
                    description = "Enemy's armor",
                    field = "DAM",
                    delta = -armor,
                });


                if(attack.range == AttackInfo.Range.Ranged && isShieldWall) {
                    attack.damage -= armor;

                    attack.modLog?.Add(new AttackModLog() {
                        description = "shield wall",
                        field = "DAM",
                        delta = -armor,
                    });
                }
            }


            if(this.isEthereal) {
                //physical attacks against ethereal units do 50% damage rounded up.
                attack.modLog?.Add(new AttackModLog() {
                    description = "ethereal",
                    field = "DAM",
                    delta = -attack.damage/2,
                });

                attack.damage -= attack.damage/2;
            }

        } else {
            attack.damage -= resistance;
            attack.modLog?.Add(new AttackModLog() {
                description = "resistance",
                field = "DAM",
                delta = -resistance,
            });
        }

        if(this.isCavalry && attack.isPolearm) {
            attack.critical += 50;

            attack.modLog?.Add(new AttackModLog() {
                description = "polearm vs cavalry",
                field = "CRIT",
                delta = 50,
            });

        }

        if(attack.damage < 0) {
            attack.damage = 0;
        }
    }

    public bool spellsChangedThisTurn = false;

    public List<UnitSpell> spells = new List<UnitSpell>();

    public class SpellCastInfo
    {
        public UnitSpell spell;
        public int round;

        public SpellCastInfo(UnitSpell spellCast)
        {
            spell = spellCast;
            round = GameController.instance.gameState.nround;
        }

        public SpellCastInfo()
        {
            round = 0;
            spell = null;
        }
    }

    List<SpellCastInfo> _spellsCast = new List<SpellCastInfo>();

    public bool HasSpellsOnCooldown {
        get {
            foreach(var spell in spells) {
                if(SpellOnCooldown(spell)) {
                    return true;
                }
            }

            return false;
        }
    }

    public int SpellCooldownRemaining(UnitSpell s)
    {
        foreach(var info in _spellsCast) {
            if(info.spell == s) {
                if(s.cooldown < 0) {
                    return int.MaxValue;
                }

                return s.cooldown - (GameController.instance.gameState.nround - info.round);
            }
        }

        return 0;
    }

    public bool SpellOnCooldown(UnitSpell s)
    {
        return SpellCooldownRemaining(s) > 0;
    }

    public void RefreshSpells()
    {
        _spellsCast.Clear();
    }

    public void PutSpellOnCooldown(UnitSpell s)
    {
        foreach(var info in _spellsCast) {
            if(info.spell == s) {
                info.round = GameController.instance.gameState.nround;
                return;
            }
        }

        _spellsCast.Add(new SpellCastInfo(s));
    }

    public void ExpendSpell(UnitSpell s)
    {
        PutSpellOnCooldown(s);
        resting = false;
        tired = true;
    }

    public List<AttackInfo> GetAttacks(bool includeDetails=false, bool permanentStats=false)
    {
        List<AttackInfo> result = new List<AttackInfo>(unitType.attacks);
        for(int i = 0; i != result.Count; ++i) {
            AttackInfo attack = result[i];
            if(includeDetails) {
                attack.modLog = new List<AttackModLog>();
            } else {
                attack.modLog = null;
            }

            var modList = permanentStats ? permanentMods : mods;

            foreach(var mod in modList) {
                foreach(var attackMod in mod.attackMods) {
                    attackMod.Apply(mod, ref attack, attack.modLog);
                }
            }

            result[i] = attack;
        }
        return result;
    }

    public List<AttackInfo> attacks {
        get {
            return GetAttacks();
        }
    }

    public List<AttackInfo> GetAttacksForBattle(UnitInfo enemy, bool attacking, bool logDetails=false)
    {
        var result = new List<AttackInfo>();
        foreach(AttackInfo a in GetAttacks(logDetails)) {
            //cannot use an attack that has limited charges if we're defending or if
            //it doesn't have any charges left.
            if(a.numCharges > 0 && (!attacking || GetAttacksExpended(a.id) >= a.numCharges)) {
                continue;
            }

            AttackInfo attackInfo = a;

            if(isFlanker) {
                int bonus = 0;
                Tile enemyTile = GameController.instance.map.GetTile(enemy.loc);
                if(enemyTile != null) {
                    foreach(Tile adjTile in enemyTile.adjacentTiles) {
                        if(adjTile != null && adjTile.unit != null && adjTile.unit.unitInfo != this && adjTile.unit.unitInfo.IsAlly(this)) {
                            bonus += 10;
                        }
                    }
                }

                if(bonus > 0) {
                    attackInfo.accuracy += bonus;
                    attackInfo.modLog?.Add(new AttackModLog() {
                        description = "flank",
                        field = "ACC",
                        delta = bonus,
                    });
                }
            }

            if(attackInfo.isAssassinate) {
                if(enemy.hasAdjacentAllies == false) {
                    attackInfo.critical += 25;
                    attackInfo.accuracy += 25;
                    attackInfo.activeEffects += "assassinate ";

                    attackInfo.modLog?.Add(new AttackModLog() {
                        description = "assassinate",
                        field = "ACC",
                        delta = 25,
                    });

                    attackInfo.modLog?.Add(new AttackModLog() {
                        description = "assassinate",
                        field = "CRIT",
                        delta = 25,
                    });

                }
            }

            if(attackInfo.isCharge) {
                Tile ourTile = GameController.instance.map.GetTile(loc);
                Tile theirTile = GameController.instance.map.GetTile(enemy.loc);
                if(ourTile.terrain.rules.chargeWorks && theirTile.terrain.rules.chargeWorks) {
                    attackInfo.critical += 50;
                    attackInfo.activeEffects += "charge ";

                    attackInfo.modLog?.Add(new AttackModLog() {
                        description = "charge",
                        field = "CRIT",
                        delta = 50,
                    });
                }
            }

            if(attacking && attackInfo.isBackstab) {
                Tile ourTile = GameController.instance.map.GetTile(loc);
                Tile[] adjacentToDefender = GameController.instance.map.GetTile(enemy.loc).adjacentTiles;
                for(int i = 0; i != adjacentToDefender.Length; ++i) {
                    if(adjacentToDefender[i] == ourTile) {
                        int opposite = (i+3)%6;
                        if(adjacentToDefender[opposite].unit != null && adjacentToDefender[opposite].unit.unitInfo.IsAlly(this)) {
                            attackInfo.critical += 50;
                            attackInfo.activeEffects += "backstab ";

                            attackInfo.modLog?.Add(new AttackModLog() {
                                description = "backstab",
                                field = "CRIT",
                                delta = 50,
                            });
                        }
                        break;
                    }
                }
            }

            if(status.Contains(GameConfig.instance.statusBlessingOfStrength)) {
                attackInfo.damage *= 2;

                attackInfo.modLog?.Add(new AttackModLog() {
                    description = "blessing",
                    field = "DAM",
                    delta = attackInfo.damage/2,
                });
            }

            attackInfo.criticalDamage = attackInfo.damage*2;

            enemy.ApplyDefense(this, ref attackInfo);
            result.Add(attackInfo);
        }

        return result;
    }

    public AttackInfo? GetBestCounterattack(UnitInfo enemy, AttackInfo attack, bool logDetails=false)
    {
        if(sleeping) {
            return null;
        }

        List<AttackInfo> counters = GetAttacksForBattle(enemy, false, logDetails);
        foreach(AttackInfo info in counters) {
            if(info.range == attack.range) {
                return info;
            }
        }
        return null;
    }

    public string hitpointsMaxCalc {
        get {
            string result = string.Format("innate: {0}", unitType.hitpointsMax);
            foreach(var mod in mods) {
                if(mod.hitpoints != 0) {
                    result += string.Format("\n{0}: {1}{2}", mod.description, mod.hitpoints > 0 ? "+" : "", mod.hitpoints);
                }
            }

            result += string.Format("\ntotal: {0}", hitpointsMax);
            return result;
        }
    }


    public int hitpointsMax {
        get {
            int result = unitType.hitpointsMax;
            foreach(var mod in mods) {
                result += mod.hitpoints;
            }
            return result;
        }
    }

    public string experienceMaxCalc {
        get {
            string result = string.Format("innate: {0}", unitType.experienceMax);
            foreach(var mod in mods) {
                if(mod.experience != 0) {
                    result += string.Format("\n{0}: {1}{2}", mod.description, mod.experience > 0 ? "+" : "", mod.experience);
                }
            }

            result += string.Format("\ntotal: {0}", experienceMax);
            return result;
        }
    }

    public int experienceMax {
        get {
            int result = unitType.experienceMax;
            foreach(var mod in mods) {
                result += mod.experience;
            }
            return result;
        }
    }

    public void InflictDamage(int amount)
    {
        if(temporaryHitpoints > 0) {
            if(temporaryHitpoints >= amount) {
                temporaryHitpoints -= amount;
                return;
            }

            amount -= temporaryHitpoints;
            temporaryHitpoints = 0;
        }

        damageTaken += amount;
    }

    public int amla = 0;

    public int temporaryHitpoints = 0;

    public int damageTaken = 0;
    public int hitpointsRemaining { get { return Mathf.Clamp(hitpointsMax - damageTaken, 0, hitpointsMax); } }

    public bool dead { get { return hitpointsRemaining <= 0; } }

    public int experience = 0;
    public void GainExperience(int amount)
    {
        if(level >= 3) {
            var teamInfo = GameController.instance.teams[nteam];
            if(teamInfo.doubleXpHighLevelUnits) {
                experience += amount;
            }
        }

        experience += amount;
    }

    public bool tired {
        get {
            return status.Contains(GameConfig.instance.statusTired);
        }
        set {
            if(tired != value) {
                if(value) {
                    status.Add(GameConfig.instance.statusTired);
                } else {
                    status.Remove(GameConfig.instance.statusTired);
                }

                statusDirty = true;
            }
        }
    }
    public bool hasAttacked = false;
    public void ExpendAttack()
    {
        hasAttacked = true;
        resting = false;
    }

    public int vision {
        get {
            int result = unitType.movement;
            foreach(var mod in mods) {
                result += mod.movement + mod.vision;
            }

            return result;
        }
    }

    public int spellRange {
        get {
            int result = 0;
            foreach(var mod in mods) {
                result += mod.spellRange;
            }

            return result;
        }
    }

    public int movement {
        get {
            int result = unitType.movement;
            foreach(var mod in mods) {
                result += mod.movement;
            }
            return result;
        }
    }

    public int movementPermanent {
        get {
            int result = unitType.movement;
            foreach(var mod in permanentMods) {
                result += mod.movement;
            }
            return result;
        }
    }


    public string movementCalc {
        get {
            string result = string.Format("innate: {0}", unitType.movement);
            foreach(var mod in mods) {
                if(mod.movement != 0) {
                    result += string.Format("\n{0}: {1}{2}", mod.description, mod.movement > 0 ? "+" : "", mod.movement);
                }
            }

            result += string.Format("\ntotal: {0}", movement);
            return result;
        }
    }

    public bool expendedVision = false;

    public void ExpendMovement(int amount)
    {
        movementExpended += amount;
        resting = false;
    }

    public int movementExpended = 0;
    public int movementRemaining {
        get {
            if(hasAttacked) {
                return 0;
            }
            return Mathf.Max(0, movement - movementExpended);
        }
    }

    //capacity to reverse exhaust, allowing us to reverse a sleep command
    int _movementExpendedBeforeExhaust = -1;
    bool _hasAttackedBeforeExhaust = true;
    bool _tiredBeforeExhaust = true;

    public void Unexhaust()
    {
        movementExpended = _movementExpendedBeforeExhaust;
        hasAttacked = _hasAttackedBeforeExhaust;
        tired = _tiredBeforeExhaust;
    }

    public void Exhaust()
    {
        _movementExpendedBeforeExhaust = movementExpended;
        _hasAttackedBeforeExhaust = hasAttacked;
        _tiredBeforeExhaust = tired;

        movementExpended = movement;
        hasAttacked = true;
        tired = true;
    }

    public bool resting = false;


    static ProfilerMarker s_profileMovecost = new ProfilerMarker("Unit.MoveCost");

    public int MoveCost(Tile dest, bool visionCost=false)
    {
        using(s_profileMovecost.Auto()) {

            if(dest.isvoid) {
                return 99;
            }

            if(CannotEnterTerrain(dest.terrain.rules)) {
                return 99;
            }

            if(isFlying) {
                return 1;
            }

            if(isAquatic && visionCost == false) {
                var rules = dest.terrain.rules;
                return rules.aquatic || rules.castle || rules.keep || rules.village ? 1 : 99;
            }

            if(AdvantageInTerrain(dest.terrain.rules)) {
                return 1;
            }

            int result = visionCost ? dest.terrain.rules.visionCost : dest.terrain.rules.moveCost;

            if(DisadvantageInTerrain(dest.terrain.rules) && visionCost == false) {
                result += (result-1);
            }

            if(result > 3) {
                result = 99;
            }

            if(result <= 3 && dest.underworldGate) {
                result = 1;
            }

            return result;
        }
    }

    public bool IsEnemy(UnitInfo otherUnit)
    {
        return GameController.instance.teams[ncontroller].team.IsEnemy(GameController.instance.teams[otherUnit.ncontroller].team);
    }

    public bool IsAlly(UnitInfo otherUnit)
    {
        return GameController.instance.teams[ncontroller].team.IsAlly(GameController.instance.teams[otherUnit.ncontroller].team);
    }

    public bool IsIndifferent(UnitInfo otherUnit)
    {
        return GameController.instance.teams[ncontroller].team.IsIndifferent(GameController.instance.teams[otherUnit.ncontroller].team);
    }

    public StoryEventInfo killedByPlayerEvent = null;

    public string DescribeOtherUnitRace(UnitInfo otherUnit)
    {
        string result = otherUnit.racialDescription;
        if(result == this.racialDescription) {
            result = string.Format("Fellow {0}", result);
        }

        return result;
    }

    public string racialDescription {
        get {
            foreach(UnitTag tag in unitType.tags) {
                if(tag.isRace) {
                    return tag.description;
                }
            }

            return "???";
        }
    }

    public bool CanCastSpell(UnitSpell spell)
    {
        bool schoolMatch = false;
        foreach(var school in spellSchoolsKnown) {
            if(spell.schools.Contains(school)) {
                schoolMatch = true;
                break;
            }
        }

        return schoolMatch && spellHighestLevelKnown >= spell.spellLevel;
    }

    public int spellHighestLevelKnown {
        get {
            int result = 0;
            foreach(var spell in this.unitType.unitInfo.spells) {
                if(spell.spellLevel > result) {
                    result = spell.spellLevel;
                }
            }

            return result;

        }
    }

    public List<SpellSchool> spellSchoolsKnown {
        get {
            if(this.unitType.spellSchoolsOverride.Count > 0) {
                return unitType.spellSchoolsOverride;
            }

            List<SpellSchool> result = new List<SpellSchool>();
            foreach(var spell in this.unitType.unitInfo.spells) {
                foreach(var s in spell.schools) {
                    if(result.Contains(s) == false) {
                        result.Add(s);
                    }
                }
            }

            return result;
        }
    }

    public Sprite avatarImage {
        get {
            return unitType.GetAnim(new AnimMatch() { gender = gender, animType = AnimType.Stand, zombietype = zombietype }).sprites[0];
        }
    }

    [System.NonSerialized]
    public bool statusDirty = false;
}

public class Unit : MonoBehaviour
{
    public Transform avatarTransform;
    public SpriteRenderer spriteRenderer { get { return _renderer; } }

    [SerializeField]
    SpriteRenderer _ellipseBottom = null, _ellipseTop = null;

    [SerializeField]
    SpriteRenderer _rulerCrown = null;

    public Dictionary<Loc, Pathfind.Path> lastCalculatedVision = null;

    bool _lastClickedUnit = false;
    public bool lastClickedUnit {
        get { return _lastClickedUnit; }
        set {
            if(value != _lastClickedUnit) {
                _ellipse.highlight = value;
                _lastClickedUnitColorMult = new Color(1f, 1f, 1f, 1f);
                RecalculateColorMult();
                _lastClickedUnit = value;
            }
        }
    }

    public UnitAITemporaryStatus aiStatus = new UnitAITemporaryStatus();

    float _floatingLabelSpawnTime = 0f;
    float _floatingLabelStaggerTime = 0.5f;
    struct FloatingLabelInfo
    {
        public string text;
        public Color color;
    }

    List<FloatingLabelInfo> _queuedFloatingLabels = new List<FloatingLabelInfo>();

    public void FloatLabel(string text, Color color)
    {
        if(gameObject.activeSelf == false) {
            return;
        }

        FloatingLabelInfo info = new FloatingLabelInfo() {
            text = text,
            color = color,
        };

        _queuedFloatingLabels.Add(info);
    }

    void UpdateFloatingLabels()
    {
        if(_queuedFloatingLabels.Count > 0 && Time.time > _floatingLabelSpawnTime + _floatingLabelStaggerTime) {
            DoFloatLabel(_queuedFloatingLabels[0]);
            _queuedFloatingLabels.RemoveAt(0);
            _floatingLabelSpawnTime = Time.time;
        }
    }

    void DoFloatLabel(FloatingLabelInfo info)
    {
        FloatingLabel label = Instantiate(GameController.instance.floatingLabelPrefab, GameController.instance.transform);
        label.transform.localPosition = transform.position;
        label.text.text = info.text;
        label.text.color = info.color;
        label.gameObject.SetActive(true);
    }

    public bool playerControlled {
        get {
            return team.player;
        }
    }

    public bool UsesZocAgainst(UnitInfo otherUnit)
    {
        return HasZonesOfControl && unitInfo.IsEnemy(otherUnit);
    }

    public bool HasZonesOfControl {
        get { return unitInfo.level > 0 && unitInfo.isInvisible == false; }
    }

    public Tile.Direction facing {
        get { return unitInfo.facing; }
        set {
            if(unitInfo.facing != value) {
                unitInfo.facing = value;

                UpdateFacing();
                AnimInfo anim = GetAnim(_currentAnimQuery);
                if(_currentAnim == null || anim != _currentAnim.anim) {
                    PlayAnimation(anim);
                }
            }
        }
    }

    void UpdateFacing()
    {
        _renderer.flipX = unitInfo.facing == Tile.Direction.NorthWest || unitInfo.facing == Tile.Direction.SouthWest;
        _currentAnimQuery.direction = unitInfo.facing;
    }

    [SerializeField]
    SpriteRenderer _renderer;

    public GameController controller { get { return GameController.instance; } }

    public Team team { get { return teamInfo.team; } }
    public TeamInfo teamInfo { get { return GameController.instance.teams[unitInfo.ncontroller]; } }

    public bool GiveUnitEquipment(Equipment equip)
    {
        if(equip.EquippableForUnit(unitInfo)) {
            unitInfo.equipment.Add(equip);
            return true;
        } else {
            teamInfo.equipmentStored.Add(equip);
            return false;
        }
    }


    [SerializeField]
    Orb _orb = null;

    [SerializeField]
    Canvas _canvas = null;

    [SerializeField]
    Image _statusIconPrefab = null;

    class StatusIconImage
    {
        public Image image;
        public Color color;
    }

    List<StatusIconImage> _statusIconInstances = new List<StatusIconImage>();

    public void WakeUp()
    {
        unitInfo.status.Remove(GameConfig.instance.statusSleep);
        unitInfo.status.Remove(GameConfig.instance.statusFallingAsleep);

        unitInfo.statusDirty = true;
    }

    public void RemoveCharmed()
    {
        unitInfo.status.Remove(GameConfig.instance.statusCharmed);
        unitInfo.statusDirty = true;
    }

    struct StatusDisplayInfo
    {
        public Sprite sprite;
        public Color color;
        public string tooltip;

        public bool Equals(StatusDisplayInfo other)
        {
            return sprite == other.sprite && color == other.color && tooltip == other.tooltip;
        }
    }

    List<StatusDisplayInfo> _currentStatusDisplayEntries = new List<StatusDisplayInfo>();

    List<StatusDisplayInfo> statusDisplayEntries {
        get {
            List<StatusDisplayInfo> result = new List<StatusDisplayInfo>();
            bool hasSpells = unitInfo.spells.Count > 0;
            bool hasNonExhaustedSpells = false;
            foreach(var spell in unitInfo.spells) {
                if(unitInfo.SpellOnCooldown(spell) == false) {
                    hasNonExhaustedSpells = true;
                }
            }
            if(hasNonExhaustedSpells == false) {
                foreach(var equip in unitInfo.equipment) {
                    if(equip.activatedAbility != null) {
                        hasSpells = true;
                        if(hasNonExhaustedSpells == false && unitInfo.SpellOnCooldown(equip.activatedAbility) == false) {
                            hasNonExhaustedSpells = true;
                        }
                    }
                }
            }

            if(hasSpells) {
                bool activated = hasNonExhaustedSpells && unitInfo.tired == false;
                StatusDisplayInfo spellInfo = new StatusDisplayInfo() {
                    sprite = GameConfig.instance.magicIcon,
                    color =  activated ? Color.white : Color.gray,
                    tooltip = activated ? "This unit has spells or abilities it can use." : (hasNonExhaustedSpells ? "This unit has spells or abilities, but has already used one this round." : "This unit has spells or abilities but they are exhausted and cannot be used again until they come off cooldown or the unit rests"),
                };
                result.Add(spellInfo);
            }


            List<string> abilitySources = unitInfo.abilitySourceDescriptions;
            List<UnitAbility> abilities = unitInfo.abilities;

            for(int i = 0; i != abilities.Count; ++i) {
                UnitAbility ability = abilities[i];
                string tooltip = UnitStatusPanel.GetAbilityTooltip(unitInfo, ability, abilitySources[i]);
                Color color = Color.white;

                if(ability.unitMod != null && ability.unitMod.useMod && ability.unitMod.filter.excludeIfFoughtThisTurn) {
                    if(unitInfo.fightsThisTurn > 0) {
                        color = Color.gray;
                        tooltip += "\n<color=#aaaaaa>Currently inactive because this unit has fought this round.</color>";
                    }
                }

                StatusDisplayInfo info = new StatusDisplayInfo() {
                    sprite = ability.icon,
                    tooltip = tooltip,
                    color = color,
                };

                result.Add(info);

            }

            foreach(UnitStatus status in unitInfo.status) {
                if(status.displayIconOnUnit == false) {
                    continue;
                }

                StatusDisplayInfo info = new StatusDisplayInfo() {
                    sprite = status.icon,
                    tooltip = string.Format("{0}: {1}", status.description, status.GetTooltip(this)),
                    color = Color.white,
                };

                result.Add(info);
            }

            return result;
        }
    }

    public void RefreshStatusDisplay()
    {
        unitInfo.statusDirty = false;

        List<StatusDisplayInfo> entries = statusDisplayEntries;

        if(entries.Count == _currentStatusDisplayEntries.Count) {
            bool isequal = true;
            for(int i = 0; i != entries.Count; ++i) {
                if(entries[i].Equals(_currentStatusDisplayEntries[i]) == false) {
                    isequal = false;
                    break;
                }
            }

            if(isequal) {
                return;
            }
        }

        _currentStatusDisplayEntries = entries;

        while(_statusIconInstances.Count < entries.Count) {
            Image icon = Instantiate(_statusIconPrefab, _canvas.transform, false);
            icon.transform.position += Vector3.down*(16f/72f)*_statusIconInstances.Count;
            _statusIconInstances.Add(new StatusIconImage() { image = icon });
        }

        for(int i = 0; i < entries.Count; ++i) {
            _statusIconInstances[i].image.sprite = entries[i].sprite;
            _statusIconInstances[i].image.color = _statusIconInstances[i].color = entries[i].color;
            UnitStatusPanel.SetTooltip(_statusIconInstances[i].image, entries[i].tooltip, new TooltipText.Options() {
                delay = 1.5f,
                useMousePosition = true,
            });

            _statusIconInstances[i].image.gameObject.SetActive(true);
        }

        for(int i = entries.Count; i < _statusIconInstances.Count; ++i) {
            _statusIconInstances[i].image.gameObject.SetActive(false);
        }

        poisonedEffect = unitInfo.poisoned;
        flyingEffect = unitInfo.isFlying;
        invisibleEffect = unitInfo.isInvisible;
    }

    [SerializeField]
    string _testSpeechText = "TESTING TEXT 1 2 3";

    [SerializeField]
    SpeechBubble _speechBubblePrefab = null;

    SpeechBubble _speechBubble = null;

    [Sirenix.OdinInspector.Button]
    public void ShowSpeechBubbleTest()
    {
        ShowSpeechBubble(_testSpeechText);
    }

    public bool canSpeak {
        get {
            return GameController.instance.IsLocGoodPositionForUnitSpeech(loc);
        }
    }

    public void ShowSpeechBubble(string text)
    {
        if(canSpeak == false) {
            return;
        }

        if(_speechBubble != null) {
            GameObject.Destroy(_speechBubble.gameObject);
            _speechBubble = null;
        }

        _speechBubble = Instantiate(_speechBubblePrefab, transform);
        _speechBubble.text = text;
    }

    public bool speaking {
        get {
            return _speechBubble != null;
        }
    }


    [SerializeField]
    StatusBar _hitpointsBar = null;

    [SerializeField]
    StatusBar _temporaryHitpointsBar = null;

    int _temporaryHitpointsMaxThisRound = 0;

    [SerializeField]
    StatusBar _experienceBar = null;

    //play voluntarily ended this unit's turn.
    bool _playerPressedSpace = false;
    public void SpacePressed()
    {
        _playerPressedSpace = true;
    }

    public UnitInfo unitInfo = new UnitInfo();
    AnimPlaying _currentAnim;
    AnimMatch _currentAnimQuery;
    float _timeUntilIdle = 100f;

    float _waterline = 0f;
    public float waterline {
        get { return _waterline; }
        set {
            if(value != _waterline) {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetFloat("_Waterline", value);
                _renderer.SetPropertyBlock(block);

                _waterline = value;
            }
        }
    }

    float _avatarElevation = 0f;

    void UpdateAvatarElevation()
    {
        if(_avatarElevation == 0f) {
            _renderer.transform.localPosition = Vector3.zero;
        } else {
            _renderer.transform.localPosition = Vector3.up * (_avatarElevation + 0.2f*_avatarElevation*Mathf.Sin(Time.time*6f));
        }
    }

    bool _flyingEffect = false;
    public bool flyingEffect {
        get { return _flyingEffect; }
        set {
            if(value != _flyingEffect) {
                _flyingEffect = value;
                DOTween.To(() => _avatarElevation, x => _avatarElevation = x, value ? 0.2f : 0f, 0.4f);
            }
        }
    }

    bool _poisonedEffect = false;
    public bool poisonedEffect {
        get { return _poisonedEffect; }
        set {
            if(value != _poisonedEffect) {
                _poisonedEffect = value;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetFloat("_Poisoned", _poisonedEffect ? 1f : 0f);
                _renderer.SetPropertyBlock(block);
            }
        }
    }

    [System.NonSerialized]
    public bool debugHighlight = false;

    [System.NonSerialized]
    public Color debugHighlightColor = new Color(0f, 0f, 1f, 0.5f);

    bool _invisibleEffect = false;
    public bool invisibleEffect {
        get {
            return _invisibleEffect;
        }
        set {
            if(value != _invisibleEffect) {
                _invisibleEffect = value;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetFloat("_Alpha", value ? 0.5f : 1f);
                _renderer.SetPropertyBlock(block);
            }
        }
    }

    Color _colorFlash = new Color(0f,0f,0f,0f);
    public Color colorFlash {
        get { return _colorFlash; }
        set {
            if(value != _colorFlash) {
                _colorFlash = value;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetColor("_Color", _colorFlash);
                _renderer.SetPropertyBlock(block);
            }
        }
    }

    Color _lastClickedUnitColorMult = new Color(1f, 1f, 1f, 1f);
    Color _colorMult = new Color(1f, 1f, 1f, 1f);
    public Color colorMult {
        get { return _colorMult; }
        set {
            if(value != _colorMult) {
                _colorMult = value;
                RecalculateColorMult();
            }
        }
    }

    void RecalculateColorMult()
    {
        Color color = _colorMult*_lastClickedUnitColorMult;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        block.SetColor("_ColorMult", color);
        _renderer.SetPropertyBlock(block);

        _hitpointsBar.colorMult = color;
        _experienceBar.colorMult = color;
        _temporaryHitpointsBar.colorMult = color;
        _orb.alpha = colorMult.a;

        block = new MaterialPropertyBlock();
        _ellipseBottom.GetPropertyBlock(block);
        block.SetColor("_ColorMult", color);
        _ellipseBottom.SetPropertyBlock(block);

        block = new MaterialPropertyBlock();
        _ellipseTop.GetPropertyBlock(block);
        block.SetColor("_ColorMult", color);
        _ellipseTop.SetPropertyBlock(block);

        if(_rulerCrown.gameObject.activeSelf) {
            _rulerCrown.color = color;
        }

        foreach(StatusIconImage image in _statusIconInstances) {
            image.image.color = color * image.color;
        }
    }

    bool _hiddenInUnderworld = false;
    public bool hiddenInUnderworld {
        get { return _hiddenInUnderworld; }
        set {
            if(_hiddenInUnderworld != value) {
                _hiddenInUnderworld = value;

                if(_isOnVisibleLoc == false) {
                    //not actually visible, so don't do anything.
                    gameObject.SetActive(false);
                    return;
                }

                gameObject.SetActive(true);

                StartCoroutine(HideInUnderworld());
            }
        }
    }

    IEnumerator HideInUnderworld()
    {
        var tween = DOTween.To(() => colorMult, x => colorMult = x, new Color(1f, 1f, 1f, hiddenInUnderworld ? 0f : 1f), 0.4f);
        yield return tween.WaitForCompletion();

        gameObject.SetActive(_hiddenInUnderworld ? false : true);
    }

    bool _isOnVisibleLoc = false;
    public bool isOnVisibleLoc {
        get { return _isOnVisibleLoc; }
        set {
            _isOnVisibleLoc = value;
            gameObject.SetActive(_isOnVisibleLoc && !hiddenInUnderworld);
        }
    }


    public void FlashHit(Color flashColor)
    {
        if(gameObject.activeSelf) {
            StartCoroutine(FlashHitCo(colorFlash));
        }
    }

    public IEnumerator FlashHitCo(Color flashColor)
    {
        colorFlash = flashColor;
        yield return new WaitForSeconds(0.05f);
        colorFlash = Color.clear;
    }

    public void FlashCrit()
    {
        if(gameObject.activeSelf) {
            StartCoroutine(FlashCritCo());
        }
    }

    public IEnumerator FlashCritCo()
    {
        colorFlash = new Color(1f, 0f, 0f, 1f);
        yield return new WaitForSeconds(0.05f);
        colorFlash = new Color(0f, 0f, 0f, 0f);
        yield return new WaitForSeconds(0.05f);
        colorFlash = new Color(1f, 0f, 0f, 1f);
        yield return new WaitForSeconds(0.05f);
        colorFlash = new Color(0f, 0f, 0f, 0f);
    }


    public void Heal(int amount)
    {
        if(unitInfo.isUnhealable) {
            return;
        }

        unitInfo.damageTaken -= amount;
        if(unitInfo.damageTaken < 0) {
            unitInfo.damageTaken = 0;
        }

        if(isOnVisibleLoc) {
            FloatLabel(string.Format("{0}", amount), Color.green);
            StartCoroutine(FlashHealCo());
        }
    }

    IEnumerator FlashHealCo()
    {
        for(int i = 0; i != 4; ++i) {
            colorFlash = new Color(0f, 1f, 0f, 0.5f);
            yield return null;
            colorFlash = new Color(0f, 0f, 0f, 0f);
            yield return null;
        }
    }

    public VillageBuilding buildingAtLoc {
        get {
            VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);
            return building;
        }
    }

    Tile _tile = null;

    public Tile tile {
        get {
            return _tile;
        }
    }

    public Loc loc {
        get { return unitInfo.loc; }
        set {
            unitInfo.loc = value;
            RefreshLocation();
        }
    }

    public Sprite avatarImage {
        get {
            return unitInfo.avatarImage;
        }
    }

    public AnimInfo GetAnim(AnimMatch query)
    {
        query.gender = unitInfo.gender;
        query.zombietype = unitInfo.zombietype;
        return unitInfo.unitType.GetAnim(query);
    }

    public bool PlayAnimation(AnimType animType, string tag="")
    {
        _currentAnimQuery = new AnimMatch() { direction = facing, animType = animType, tag = tag };
        return PlayAnimation(GetAnim(_currentAnimQuery));
    }

    public bool PlayAnimation(AnimInfo anim)
    {
        if(anim.valid) {
            _currentAnim = new AnimPlaying(anim);
            _timeUntilIdle = Random.Range(20f, 50f);
            _renderer.sprite = _currentAnim.sprite;
            return true;
        }

        return false;
    }

    public void PlayCastAnim()
    {
        PlayAnimation(AnimType.Cast);
    }

    public bool IsIndifferent(Unit other)
    {
        return team.IsIndifferent(other.team);
    }

    public bool IsAlly(Unit other)
    {
        return team.IsAlly(other.team);
    }

    public bool IsOnSameTeam(Unit other)
    {
        return team == other.team;
    }

    public bool IsEnemy(Unit other)
    {
        return team.IsEnemy(other.team);
    }

    public bool ShouldEnterDiplomacy(Unit other)
    {
        return WantsContact(other) || (IsEnemy(other) == false && other.team != team);
    }

    public bool WantsContact(Unit other)
    {
        return other.team.player && this.teamInfo.wantsPlayerContact;
    }

    public List<Unit> PossibleAttacks()
    {
        return PossibleAttacks(loc);
    }

    public List<Unit> PossibleAttacks(Loc loc)
    {
        List<Unit> result = new List<Unit>();

        if(unitInfo.hasAttacked) {
            return result;
        }

        Tile tile = GameController.instance.map.GetTile(loc);
        foreach(Tile.Edge edge in tile.edges) {
            Tile adj = edge.dest;

            if(adj == null) {
                continue;
            }

            if(adj.unit != null && !adj.unit.IsOnSameTeam(this)) {
                result.Add(adj.unit);
            }
        }

        return result;
    }

    public void RefreshLocation()
    {
        if(_tile != null && _tile.unit == this) {
            _tile.unit = null;
        }

        _tile = GameController.instance.map.GetTile(unitInfo.loc);
        if(_tile != null) {
            _tile.unit = this;
        }

        spriteRenderer.sortingOrder = -unitInfo.loc.y*2;
        _ellipseBottom.sortingOrder = spriteRenderer.sortingOrder+1;

        waterline = unitInfo.isFlying ? 0f : _tile.waterline;

        transform.position = tile.unitPos;
        isOnVisibleLoc = tile.fog.atLeastPartlyVisibleToPlayer;
    }

    [SerializeField]
    UnitEllipse _ellipse = null;

    void SetTeamColorHue()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        block.SetFloat("_TeamColorHue", team.coloring.hue);
        _renderer.SetPropertyBlock(block);

        _ellipse.SetColorHue();
    }

    public void SetTeam(int n)
    {
        unitInfo.nteam = n;
        SetTeamColorHue();
    }

    public void BeginTurn()
    {
        _playerPressedSpace = false;
        _temporaryHitpointsMaxThisRound = 0;
        unitInfo.BeginTurn();
        unitInfo.StatusTurnBoundary();

        VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);
        if(building != null && building.acceleratesResting) {
            CheckAwakeFromResting();
        }

        foreach(var equip in unitInfo.equipment) {
            if(equip.beginTurnEffect != null) {
                equip.beginTurnEffect.CompleteCasting(this, this.loc);
            }
        }

        unitInfo.statusDirty = true;

        if(unitInfo.isTemporal && GameController.instance.gameState.nround >= unitInfo.roundCreated+teamInfo.temporalUnitDuration) {
            GameController.instance.CheckUnitDeath(this, true);
        }
    }

    void CheckAwakeFromResting()
    {
        if(unitInfo.status.Contains(GameConfig.instance.statusFallingAsleep)) {
            unitInfo.status.Remove(GameConfig.instance.statusFallingAsleep);
            unitInfo.status.Add(GameConfig.instance.statusSleep);
        } else if(unitInfo.status.Contains(GameConfig.instance.statusSleep)) {
            unitInfo.status.Remove(GameConfig.instance.statusSleep);
            if(unitInfo.resting) {
                unitInfo = unitInfo.RefreshFromUnitType(unitInfo.unitType);

                VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);

                if(building != null) {
                    building.UnitFinishResting(this);
                }
            }
        } else {
            unitInfo.resting = false;
        }
    }

    public void EndTurn()
    {
        unitInfo.StatusTurnBoundary();

        unitInfo.EndTurn();

        CheckAwakeFromResting();

        VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);

        if(building != null) {
            building.UnitEndTurn(this);
        }

        unitInfo.statusDirty = true;
    }

    public void ApplyStatus(UnitStatus status)
    {
        bool applying = false;
        if(unitInfo.status.Contains(status) == false) {
            applying = true;
            unitInfo.status.Add(status);
        }

        if(unitInfo.statusRemoveEndOfTurn.Contains(status)) {
            applying = true;
            unitInfo.statusRemoveEndOfTurn.Remove(status);
        }

        unitInfo.statusDirty = true;

        if(applying && string.IsNullOrEmpty(status.applySlogan) == false) {
            FloatLabel(status.applySlogan, Color.red);
        }
    }

    public void SetSummoningSickness()
    {
        unitInfo.hasAttacked = true;
        unitInfo.resting = true;
        unitInfo.tired = true;
    }

    public bool dieAnimFinished = false;

    public void Die()
    {
        ClearFamiliars();

        if(gameObject.activeSelf == false) {
            GameObject.Destroy(gameObject);
        } else {
            StartCoroutine(DieCo());
        }
    }

    IEnumerator DieCo()
    {
        PlayAnimation(AnimType.Die);
        while(_currentAnim.animType == AnimType.Die && _currentAnim.finished == false) {
            yield return null;
        }

        foreach(CanvasRenderer r in GetComponentsInChildren<CanvasRenderer>()) {
            DOTween.To(() => r.GetColor(), x => r.SetColor(x), new Color(1f, 1f, 1f, 0f), 0.5f);
        }

        var tween = DOTween.To(() => colorMult, x => colorMult = x, new Color(1f,1f,1f,0f), 0.5f);
        yield return tween.WaitForCompletion();

        dieAnimFinished = true;

        GameObject.Destroy(gameObject);
    }

    public void SummonAnim()
    {
        foreach(CanvasRenderer r in GetComponentsInChildren<CanvasRenderer>()) {
            DOTween.To(() => r.GetColor(), x => r.SetColor(x), new Color(1f, 1f, 1f, 1f), 0.5f).From(new Color(1f, 1f, 1f, 0f));
        }

        DOTween.To(() => colorMult, x => colorMult = x, new Color(1f, 1f, 1f, 1f), 0.5f).From(new Color(1f, 1f, 1f, 0f));
    }

    public bool hasEnoughExperienceToLevelUp {
        get { return unitInfo.experience >= unitInfo.experienceMax; }
    }

    public bool canLevelUp {
        get { return hasEnoughExperienceToLevelUp && controller.map.GetTile(loc).terrain.rules.canLongRest && controller.currentTeamNumber == unitInfo.nteam; }
    }

    public bool canRest {
        get { return unitInfo.isTemporal == false && unitInfo.sleeping == false && controller.map.GetTile(loc).terrain.rules.canLongRest && controller.currentTeamNumber == unitInfo.nteam; }
    }

    public bool canCancelRest {
        get {
            return unitInfo.resting && unitInfo.spellsChangedThisTurn == false && unitInfo.status.Contains(GameConfig.instance.statusFallingAsleep);
        }
    }

    public bool canChangeSpells {
        get {
            if(unitInfo.spells.Count <= 0) {
                return false;
            }

            if(unitInfo.resting) {
                return true;
            }

            VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(loc);
            if(building != null && building.allowsChangingSpells) {
                return true;
            }

            return false;
        }
    }

    public bool inCastleWeOwn {
        get {
            Tile t = controller.map.GetTile(loc);
            //bit hacky at the moment. TODO: work out what 'owning' a castle means.
            bool incastle = t.terrain.rules.canLongRest;
            if(incastle == false) {
                return false;
            }

            if(t.terrain.rules.keep) {
                return true;
            }

            foreach(Tile adj in t.adjacentTiles) {
                if(adj != null && adj.terrain.rules.keep && GameController.instance.gameState.GetTeamOwnerOfLoc(adj.loc) == team) {
                    return true;
                }
            }

            return false;
        }
    }

    public Orb.Status movementStatus {
        get {
            if(team.IsEnemy(GameController.instance.playerTeam)) {
                return Orb.Status.Enemy;
            } else if(team != GameController.instance.playerTeam) {
                return team.IsAlly(GameController.instance.playerTeam) ? Orb.Status.Ally : Orb.Status.Indifferent;
            } else if(_playerPressedSpace) {
                return Orb.Status.Exhausted;
            } else if(unitInfo.movementRemaining >= unitInfo.movement) {
                return Orb.Status.Unmoved;
            } else if(unitInfo.movementRemaining > 0 || PossibleAttacks().Count > 0) {
                return Orb.Status.PartMoved;
            } else {
                return Orb.Status.Exhausted;
            }
        }
    }

    public void ClearFamiliars()
    {
        foreach(string guid in unitInfo.familiarGuids) {
            Unit familiar = GameController.instance.GetUnitByGuid(guid);
            if(familiar != null) {
                GameController.instance.CheckUnitDeath(familiar, true);
            }
        }

        unitInfo.familiarGuids.Clear();
    }

    public Unit summoner {
        set {
            if(value == null) {
                unitInfo.summonerGuid = "";
            } else {
                unitInfo.summonerGuid = value.unitInfo.guid;
            }
        }
        get {
            if(string.IsNullOrEmpty(unitInfo.summonerGuid)) {
                return null;
            }
            return GameController.instance.GetUnitByGuid(unitInfo.summonerGuid);
        }
    }

    public Unit summonerOrSelf {
        get {
            Unit result = summoner;
            if(result == null) {
                result = this;
            }

            return result;
        }
    }

    public void AwardExperience(int namount)
    {
        summonerOrSelf.unitInfo.GainExperience(namount);
    }

    // Start is called before the first frame update
    void Start()
    {
        _rulerCrown.gameObject.SetActive(unitInfo.ruler);

        SetTeamColorHue();

        UpdateFacing();

        bool animPlayed = PlayAnimation(AnimType.Stand);
        if(animPlayed == false) {
            Debug.LogError("Could not play stand animation for " + unitInfo.unitType.description);
            gameObject.SetActive(false);
        }

        RefreshLocation();
    }

    public void MoveAboveDeathLayer()
    {
        spriteRenderer.sortingLayerName = "AboveDeathLayer";
    }

    SpecialEffectUnit _levelUpEffect = null;

    public void PlayLevelUpEffect()
    {
        if(_levelUpEffect != null) {
            _levelUpEffect.Finish();
        }
    }

    int _nupdate = 0;
    // Update is called once per frame
    void Update()
    {
        if(_nupdate++%10 == 0 || unitInfo.statusDirty) {
            RefreshStatusDisplay();
        }

        if(debugHighlight) {
            float t = Time.time*2f;
            t -= Mathf.Floor(t);
            colorFlash = t < 0.5f ? debugHighlightColor : Color.clear;
        }

        if(unitInfo.temporaryHitpoints > _temporaryHitpointsMaxThisRound) {
            _temporaryHitpointsMaxThisRound = unitInfo.temporaryHitpoints;
        }

        if(_lastClickedUnit) {
            float r = 1.2f + Mathf.Sin(Time.time*4f)*0.2f;
            _lastClickedUnitColorMult = new Color(r, r, r, 1f);
            RecalculateColorMult();
        }

        UpdateFloatingLabels();
        UpdateAvatarElevation();
        
        //manage whether we're showing a level up special effect.
        if(_levelUpEffect != null && hasEnoughExperienceToLevelUp == false) {
            _levelUpEffect.Finish();
            _levelUpEffect = null;
        } else if(_levelUpEffect == null && hasEnoughExperienceToLevelUp) {
            _levelUpEffect = Instantiate(GameConfig.instance.specialEffects.levelUpEffect, transform);
            _levelUpEffect.unit = this;
        }

        if(_currentAnim == null) {
            Debug.LogError("Could not find anim for " + unitInfo.unitType.description);
            return;
        }

        _currentAnim.Step(Time.deltaTime);
        _renderer.sprite = _currentAnim.sprite;

        _hitpointsBar.targetValue = unitInfo.hitpointsRemaining;
        _hitpointsBar.targetMax = unitInfo.hitpointsMax + _temporaryHitpointsMaxThisRound;
        if(unitInfo.temporaryHitpoints <= 0) {
            _hitpointsBar.targetMaxColor = -1;
        } else {
            _hitpointsBar.targetMaxColor = unitInfo.hitpointsMax;
        }

        _temporaryHitpointsBar.targetValue = unitInfo.hitpointsRemaining + unitInfo.temporaryHitpoints;
        _temporaryHitpointsBar.targetMax = unitInfo.hitpointsMax + _temporaryHitpointsMaxThisRound;

        _experienceBar.gameObject.SetActive(unitInfo.experience > 0);
        _experienceBar.targetValue = unitInfo.experience;
        _experienceBar.targetMax = unitInfo.experienceMax;

        _orb.status = movementStatus;
        
        if(_currentAnim.finished && _currentAnim.animType != AnimType.Die) {
            PlayAnimation(AnimType.Stand);
        }

        if(_currentAnim.timePlaying >= _timeUntilIdle && _currentAnim.animType == AnimType.Stand) {
            PlayAnimation(AnimType.Idle);
        }
    }

    public void RefreshUnitInfo(UnitInfo info)
    {
        bool locChanged = unitInfo.loc != info.loc;
        unitInfo = info;
        if(locChanged) {
            RefreshLocation();
        }
    }
}
