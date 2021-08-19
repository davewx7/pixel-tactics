using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/RetrieveItem")]
public class QuestRetrieveItem : Quest
{
    public DiplomacyNode offerNode;
    public Equipment equipment;

    public override QuestType questType {
        get { return QuestType.RetrieveItem; }
    }

    public override bool IsEligible(Team clientTeam)
    {
        foreach(TeamInfo t in GameController.instance.gameState.teams) {
            //only allow one quest like this.
            if(t.currentQuest != null && t.currentQuest.quest == this) {
                return false;
            }

            foreach(var q in t.completedQuests) {
                if(q.quest == this) {
                    return false;
                }
            }
        }

        return FindLoc(clientTeam).valid;
    }


    public override float ScoreQuest(QuestInProgress info)
    {
        return 100.0f;
    }

    public override string GetDetails(QuestInProgress info)
    {
        return string.Format("Find the body of the fallen warrior near {0} and retrieve the {1} from it.", info.itemRetrieveLoc.ToString(), info.itemToRetrieve.description);
    }

    public override string GetAspiration(QuestInProgress info)
    {
        return "I will retrieve the amulet";
    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.itemRetrieveLoc = FindLoc(clientTeam);
        questInProgress.itemToRetrieve = equipment;


        questInProgress.count = 0;
        questInProgress.countNeeded = 1;
    }

    public override void OnAcceptQuest(QuestInProgress questInProgress)
    {
        Tile t = GameController.instance.map.GetTile(questInProgress.itemRetrieveLoc);
        if(t != null) {
            t.AddLoot(GameConfig.instance.deadBodyLoot, new LootInfo() {
                description = "An amulet from the body of " + questInProgress.clientTeam.teamNameAsProperNoun + " warrior.",
                equipment = new List<Equipment>() { equipment },
            });
        }
    }

    public List<DiplomacyNode> checkinHaveItem = new List<DiplomacyNode>();

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        if(HaveItem(questInProgress)) {
            return checkinHaveItem;
        } else {
            return base.GetCustomCheckin(questInProgress);
        }
    }

    public bool HaveItem(QuestInProgress questInProgress)
    {
        foreach(Unit unit in GameController.instance.units) {
            if(unit.unitInfo.equipment.Contains(questInProgress.itemToRetrieve)) {
                return true;
            }
        }

        return false;
    }

    public override bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        if(questInProgress.timeUntilExpired > 0) {
            return false;
        }

        if(questInProgress.timeUntilExpired <= -4) {
            return true;
        }

        if(HaveItem(questInProgress)) {
            return false;
        }

        return true;
    }



    Loc FindLoc(Team clientTeam)
    {
        List<Loc> playerUnitLocs = new List<Loc>();

        foreach(Unit unit in GameController.instance.GetUnitsOnTeam(GameController.instance.numPlayerTeam)) {
            playerUnitLocs.Add(unit.loc);
        }

        List<Loc> eligibleLocs = new List<Loc>();

        foreach(Tile tile in GameController.instance.map.tiles) {
            if(clientTeam.rulerType.unitInfo.MoveCost(tile) > 1) {
                continue;
            }

            if(GameController.instance.map.ocean.Contains(tile.loc)) {
                //If this location is in the ocean make sure it's at least on the coast.
                Loc[] adj = Tile.AdjacentLocs(tile.loc);
                bool coastal = false;
                foreach(Loc a in adj) {
                    if(GameController.instance.map.ocean.Contains(tile.loc) == false) {
                        coastal = true;
                        break;
                    }
                }

                if(coastal == false) {
                    continue;
                }
            }

            if(tile.revealed || tile.loot != null) {
                continue;
            }

            if(tile.unit != null) {
                continue;
            }

            if(Tile.DistanceBetween(tile.loc, clientTeam.teamInfo.keepLoc) < 24) {
                continue;
            }

            if(tile.terrain.rules.village) {
                continue;
            }

            if(tile.terrain.rules.castle) {
                continue;
            }

            bool tooCloseToKeep = false;
            foreach(TeamInfo t in GameController.instance.teams) {
                if(Tile.DistanceBetween(t.keepLoc, tile.loc) <= 6) {
                    tooCloseToKeep = true;
                }
            }

            if(tooCloseToKeep) {
                continue;
            }

            int minDistance = 999;
            foreach(Loc unitLoc in playerUnitLocs) {
                int dist = Tile.DistanceBetween(unitLoc, tile.loc);
                if(dist < minDistance) {
                    minDistance = dist;
                }
            }

            if(minDistance > 16 && minDistance < 24) {
                eligibleLocs.Add(tile.loc);
            }
        }

        if(eligibleLocs.Count == 0) {
            return Loc.invalid;
        }

        return eligibleLocs[GameController.instance.rng.Range(0, eligibleLocs.Count)];
    }

}
