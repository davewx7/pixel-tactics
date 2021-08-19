using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Info")]
public class BoonInfo : Boon
{
    public class Info
    {
        public string description;
        public Dictionary<string, TooltipText.Options> tooltips = new Dictionary<string, TooltipText.Options>();
        public List<Loc> locs;

        public TeamInfo teamInfo;
    }

    Tile GetTile(Loc loc)
    {
        return GameController.instance.map.GetTile(loc);
    }

    bool TileHidden(Tile tile)
    {
        if(tile.revealed) {
            return false;
        }

        foreach(Tile t in tile.adjacentTiles) {
            if(t != null && t.revealed) {
                return false;
            }
        }

        return true;
    }

    List<Info> GetInfo(Unit unit)
    {
        List<Info> result = new List<Info>();
        foreach(TeamInfo teamInfo in GameController.instance.teams) {
            Tile keepTile = GetTile(teamInfo.keepLoc);
            if(teamInfo.keepLoc.valid && TileHidden(keepTile)) {
                string description;

                if(teamInfo.team.barbarian) {
                    description = string.Format("There is an encampment of bandits nearby, terrorizing the nearby countryside.");
                } else {
                    description = string.Format("The <color=#ffffff>{0}</color> have their castle at <color=#ffffff>{1}</color>.", teamInfo.team.teamName, keepTile.GetLabelText());
                }

                result.Add(new Info() {
                    locs = new List<Loc>() { teamInfo.keepLoc },
                    description = description,
                    teamInfo = teamInfo.team.barbarian ? null : teamInfo,
                });
            }
        }

        foreach(Tile tile in GameController.instance.map.tiles) {
            if(tile.underworldGate && TileHidden(tile)) {
                string rumors = "are both monsters and treasures within";
                Dictionary<string, TooltipText.Options> tooltips = new Dictionary<string, TooltipText.Options>();


                DungeonInfo dungeon = null;
                foreach(DungeonInfo d in GameController.instance.gameState.dungeonInfo) {
                    if(d.entryLoc.toOverworld == tile.loc.toOverworld) {
                        dungeon = d;
                        break;
                    }
                }

                if(dungeon != null) {
                    rumors = string.Format("are <link=\"dungeon_description\">{0}</link> lurking within", dungeon.monsterDescription);
                    tooltips.Add("dungeon_description", new TooltipText.Options() {
                        text = dungeon.monsterTooltip,
                        linkNormalColor = new Color(1f, 0.7f, 0.7f),
                    });
                }

                result.Add(new Info() {
                    locs = new List<Loc>() { tile.loc },
                    description = string.Format("You learn of a deep, dark cave nearby. Rumors are that there {0}.", rumors),
                    tooltips = tooltips,
                });
            }
        }

        var finalResult = new List<Info>();
        foreach(var info in result) {
            int distance = Tile.DistanceBetween(unit.loc, info.locs[0]);
            if(distance < 15) {
                finalResult.Add(info);
            }
        }

        Debug.Log("INFO_OPTIONS: " + finalResult.Count);

        return finalResult;
    }

    public override bool IsEligible(Unit unit)
    {
        return GetInfo(unit).Count > 0;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        List<Info> possibleInfo = GetInfo(unit);
        Info infoItem = possibleInfo[info.seed%possibleInfo.Count];

        if(infoItem.teamInfo != null && infoItem.teamInfo.currentQuests.Count == 0) {
            QuestInProgress quest = DiplomacyCommand.AssignQuest(infoItem.teamInfo.team);
            if(quest != null) {
                string hint = quest.quest.QuestHint(quest);
                if(hint != null) {
                    infoItem.teamInfo.currentQuests.Add(quest);
                    infoItem.description += "\n" + hint;
                }
            }
        }

        GenericCommandInfo cmd = GameController.instance.QueueGenericCommand();
        AIDiplomacyManager.instance.StartCoroutine(AIDiplomacyManager.instance.ShareInfo(infoItem, cmd));
    }

}
