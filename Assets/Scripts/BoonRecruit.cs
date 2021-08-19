using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Recruit")]
public class BoonRecruit : Boon
{
    [System.Serializable]
    public class Entry
    {
        public bool debugPriority;
        public UnitType unitType;

        public UnitInfo CreateUnit(int nseed)
        {
            var unitInfo = unitType.createUnit(nseed);
            ApplyOverrides(unitInfo);

            return unitInfo;
        }

        public void ApplyOverrides(UnitInfo unitInfo)
        {
            if(string.IsNullOrEmpty(characterName) == false) {
                unitInfo.characterName = characterName;
            }

            if(spellsOverride.Count > 0) {
                unitInfo.spells = new List<UnitSpell>(spellsOverride);
            }

            unitInfo.characterMods = new List<UnitMod>(characterMods);

            if(equipment.Count > 0) {
                unitInfo.equipment = new List<Equipment>(equipment);
            }
        }

        public string characterName;

        public List<UnitSpell> spellsOverride;
        public List<UnitMod> characterMods;
        public List<Equipment> equipment;


        [TextArea(3, 3)]
        public string dialogStoryLine;

        [TextArea(3, 3)]
        public string recruitStoryLine;

        public string confirmStoryText;

        public List<TerrainRules> adjacentTerrain;

        public List<Team> enemyRequired;

        public Team GetEnemy(Unit recruitingUnit)
        {
            foreach(Team candidateEnemy in enemyRequired) {
                if(candidateEnemy.IsTeamInGame && recruitingUnit.team.IsEnemy(candidateEnemy)) {
                    return candidateEnemy;
                }
            }

            return null;
        }

    }

    [SerializeField]
    List<Entry> _entries = null;


    string CustomizeStringForUnit(string str, Unit recruitingUnit, Entry entry, UnitInfo unitInfo)
    {
        string result = str;
        Debug.Log("Customizing string: " + result + " FOR GENDER: " + unitInfo.gender.ToString());
        result = result.Replace("{name}", unitInfo.characterName);
        result = result.Replace("{type}", unitInfo.unitType.classDescription);

        Team enemy = entry.GetEnemy(recruitingUnit);
        if(enemy != null) {
            result = result.Replace("{enemy_team}", enemy.teamNameAsProperNoun);
        }

        var gender = unitInfo.gender;
        if(gender == UnitGender.Male) {
            result = result.Replace("{him}", "him");
            result = result.Replace("{Him}", "Him");

            result = result.Replace("{his}", "his");
            result = result.Replace("{His}", "His");

            result = result.Replace("{he}", "he");
            result = result.Replace("{He}", "He");

            result = result.Replace("{man}", "man");

        } else if(gender == UnitGender.Female) {
            result = result.Replace("{him}", "her");
            result = result.Replace("{Him}", "Her");

            result = result.Replace("{his}", "her");
            result = result.Replace("{His}", "Her");
                                     
            result = result.Replace("{he}", "she");
            result = result.Replace("{He}", "She");
                                     
            result = result.Replace("{man}", "woman");


        } else {
            result = result.Replace("{him}", "it");
            result = result.Replace("{Him}", "It");

            result = result.Replace("{his}", "its");
            result = result.Replace("{His}", "its");

            result = result.Replace("{he}", "it");
            result = result.Replace("{He}", "It");

            result = result.Replace("{man}", "creature");

        }

        Debug.Log("Customizing string RETURN RESULT: " + result);

        return result;
    }

    public override string GetDialogStoryline(Unit unit, int nseed)
    {
        var entry = GetEntry(unit, nseed);
        string result = entry.dialogStoryLine;
        var unitInfo = entry.CreateUnit(nseed);
        result = CustomizeStringForUnit(result, unit, entry, unitInfo);

        return result;
    }

    public override string GetStoryText(Unit unit, int nseed)
    {
        var entry = GetEntry(unit, nseed);
        var unitInfo = entry.CreateUnit(nseed);
        string storyText = entry.confirmStoryText;
        return CustomizeStringForUnit(storyText, unit, entry, unitInfo);
    }


    public override Sprite GetAvatarSprite(Unit unit, int nseed)
    {
        return GetEntry(unit, nseed).CreateUnit(nseed).portrait;
    }

    List<Entry> GetPossibleUnits(Unit unit)
    {
        List<Entry> result = new List<Entry>();

        bool requiresPriority = false;
        
        foreach(Entry entry in _entries) {
            var unitType = entry.unitType;
            if(unit.teamInfo.joinOffers.Contains(unitType)) {
                //won't have the same type of unit offer to join more than once per game.
                continue;
            }

            if(unit.teamInfo.recruitTypes.Contains(unitType)) {
                //Giving the player a unit they can already recruit is boring.
                continue;
            }

            foreach(Unit u in GameController.instance.units) {
                if(u.team == unit.team && u.unitInfo.unitType == unitType) {
                    //the player already controls a unit of this type.
                    continue;
                }
            }

            if(entry.enemyRequired.Count > 0 && entry.GetEnemy(unit) == null) {
                continue;
            }

            //aquatic units only offered if we are on the coast.
            if(unitType.unitInfo.isAquatic && GameController.instance.map.AdjacentToOcean(unit.loc) == false) {
                continue;
            }

            if(entry.adjacentTerrain.Count > 0) {
                bool terrainMatches = false;
                foreach(Tile adj in unit.tile.adjacentTiles) {
                    if(entry.adjacentTerrain.Contains(adj.terrain.rules)) {
                        terrainMatches = true;
                    }
                }

                if(terrainMatches == false) {
                    continue;
                }
            }

            if(requiresPriority && entry.debugPriority == false) {
                continue;
            }

            if(entry.debugPriority) {
                if(requiresPriority == false) {
                    result.Clear();
                    requiresPriority = true;
                }
            }

            result.Add(entry);
        }

        return result;
    }

    public override bool IsEligible(Unit unit)
    {
        return GetPossibleUnits(unit).Count > 0;
    }

    //precondition: IsEligible() returned true.
    Entry GetEntry(Unit unit, int nseed)
    {
        var candidates = GetPossibleUnits(unit);
        return candidates[nseed%candidates.Count];
    }

    public override void RecordOffer(int nseed, Unit unit, bool accepted)
    {
        UnitType unitType = GetEntry(unit, nseed).unitType;
        unit.teamInfo.joinOffers.Add(unitType);
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        var entry = GetEntry(unit, info.seed);
        UnitType unitType = entry.unitType;
        Loc spawnLoc = GameController.instance.FindVacantTileNear(unit.loc, unitType.unitInfo);
        if(spawnLoc.valid) {
            var commandInfo = new RecruitCommandInfo() {
                unitType = unitType,
                loc = spawnLoc,
                seed = info.seed,
                recruitEntry = entry,
                haveHaste = true,
            };

            Unit fakeUnit = RecruitCommand.SummonUnit(commandInfo, true); //dry run the recruit so we can get unit info.

            GameController.instance.ExecuteRecruit(commandInfo);



            //spawn.SetSummoningSickness();
            string storyText = CustomizeStringForUnit(entry.recruitStoryLine, unit, entry, fakeUnit.unitInfo);
            Sprite portrait = fakeUnit.unitInfo.portrait;
            GameObject.Destroy(fakeUnit.gameObject);

            GameController.instance.ShowDialogMessage("A New Recruit", storyText, fakeUnit.unitInfo.portrait);
        }
    }
}
