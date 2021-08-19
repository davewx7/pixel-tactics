using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/ClearDungeon")]
public class QuestClearDungeon : Quest
{
    public DiplomacyNode offerNode;

    public override QuestType questType {
        get { return QuestType.ClearDungeon; }
    }

    public DungeonInfo GetDungeonInfo(Team clientTeam)
    {
        DungeonInfo result = null;
        int bestDist = -1;
        TeamInfo clientTeamInfo = clientTeam.teamInfo;
        foreach(DungeonInfo dungeon in GameController.instance.gameState.dungeonInfo) {
            int dist = Tile.DistanceBetween(dungeon.entryLoc.toOverworld, clientTeamInfo.keepLoc);
            if(result == null || dist < bestDist) {
                result = dungeon;
                bestDist = dist;
            }
        }

        return result;
    }

    public override bool IsEligible(Team clientTeam)
    {
        DungeonInfo dungeon = GetDungeonInfo(clientTeam);
        return dungeon != null;
    }

    public DungeonInfo GetDungeon(QuestInProgress info)
    {
        if(string.IsNullOrEmpty(info.dungeonGuid)) {
            return null;
        }

        foreach(DungeonInfo dungeon in GameController.instance.gameState.dungeonInfo) {
            if(dungeon.guid == info.dungeonGuid) {
                return dungeon;
            }
        }

        return null;
    }

    public override float ScoreQuest(QuestInProgress info)
    {
        //slightly higher priority than other quest types 'by default'
        //but can easily drop if the dungeon isn't ideally situated.
        float score = 110f;

        foreach(TeamInfo otherTeam in GameController.instance.teams) {
            if(otherTeam.team != info.clientTeam) {
                foreach(QuestInProgress q in otherTeam.currentQuests) {
                    if(q.dungeonGuid == info.dungeonGuid) {
                        //another team has already asked to clear this dungeon.
                        return 0f;
                    }

                    if(q.quest is QuestClearDungeon) {
                        score -= 25f;
                    }
                }
            }
        }

        DungeonInfo dungeon = GetDungeon(info);
        if(dungeon == null) {
            return 0f;
        }

        int distance = Tile.DistanceBetween(dungeon.entryLoc, info.clientTeam.teamInfo.keepLoc);
        if(distance > 12) {
            score -= (distance-12)*5f;
        }

        Unit playerRuler = GameController.instance.playerTeamInfo.GetRuler();
        if(playerRuler != null) {
            //don't ask the player to go to a dungeon an excessive distance from them.
            int distanceFromPlayer = Tile.DistanceBetween(playerRuler.loc, dungeon.entryLoc);
            if(distanceFromPlayer > 20) {
                score -= (distanceFromPlayer-20)*3f;
            }
        }

        return score;
    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        DungeonInfo dungeon = GetDungeonInfo(clientTeam);
        if(dungeon != null) {
            questInProgress.dungeonGuid = dungeon.guid;
        }

        questInProgress.progressEstimateMax = 3;
    }

    public override void OnUnitArrivesAtLoc(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.team.player && unit.loc.underworld && questInProgress.progressEstimate < 1) {
            DungeonInfo dungeon = GameController.instance.gameState.GetDungeon(questInProgress.dungeonGuid);
            if(dungeon.interiorLocs.Contains(unit.loc)) {
                questInProgress.progressEstimate = 1;
            }
        }
    }

    public override void OnEnemyUnitKilled(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.loc.underworld && unit.team.player == false) {
            DungeonInfo dungeon = GameController.instance.gameState.GetDungeon(questInProgress.dungeonGuid);
            if(dungeon.interiorLocs.Contains(unit.loc)) {
                questInProgress.progressEstimate++;
            }
        }
    }

    public override string GetDetails(QuestInProgress info)
    {
        DungeonInfo dungeon = GetDungeon(info);

        if(dungeon == null) {
            return "Destroy all monsters within the dungeon";
        }

        string monsterDescription = dungeon.monsterDescription;

        return string.Format("Enter the dungeon at {0} and destroy {1} within.", dungeon.entryLoc, monsterDescription);
    }

    public override string AchievementText(QuestInProgress info)
    {
        return "Clearing the Dungeon";
    }

    public override string GetCompletionDescription(QuestInProgress info)
    {
        return "the dungeon full of those who were harassing us has been cleared";
    }

    public override string QuestHint(QuestInProgress questInProgress)
    {
        DungeonInfo dungeon = GetDungeon(questInProgress);

        string monsterDescription = dungeon.monsterDescription;

        return string.Format("Word is that there is a dungeon full of {0} near their castle. Every Winter they come forth, ravaging the countryside. They have been looking for someone to help with this threat.", monsterDescription);
    }

    public List<DiplomacyNode> noProgressAlmostExpired;

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        if(AlmostFailed(questInProgress)) {
            return noProgressAlmostExpired;
        } else {
            return base.GetCustomCheckin(questInProgress);
        }
    }

    public override bool AlmostFailed(QuestInProgress questInProgress)
    {
        return questInProgress.progressEstimate < questInProgress.progressEstimateMax && questInProgress.timeUntilExpired <= 4;
    }

    public override bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        return (questInProgress.timeUntilExpired <= 0 && questInProgress.progressEstimate < questInProgress.progressEstimateMax) || questInProgress.timeUntilExpired <= -4;
    }
}
