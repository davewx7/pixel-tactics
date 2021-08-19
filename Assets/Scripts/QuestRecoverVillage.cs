using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/RecoverVillage")]
public class QuestRecoverVillage : Quest
{
    public List<UnitType> summonUnits = new List<UnitType>();

    public DiplomacyNode offerNode;

    public override QuestType questType {
        get { return QuestType.RecoverVillage; }
    }

    Tile FindTargetVillage(Team clientTeam)
    {
        Loc playerKeep = GameController.instance.playerTeamInfo.keepLoc;
        Loc playerRulerLoc = GameController.instance.playerTeamInfo.GetRuler().loc;
        Loc clientKeep = clientTeam.teamInfo.keepLoc;

        Tile bestResult = null;
        int bestScore = 0;

        foreach(Tile tile in GameController.instance.map.tiles) {
            if(tile.terrain.rules.village == false || tile.unit != null || tile.revealed || GameController.instance.gameState.GetTeamOwnerOfLoc(tile.loc) != null || string.IsNullOrEmpty(tile.GetLabelText())) {
                continue;
            }

            int playerDistToVillage = Mathf.Min(Tile.DistanceBetween(playerRulerLoc, tile.loc), Tile.DistanceBetween(playerKeep, tile.loc));
            int clientDistToVillage = Tile.DistanceBetween(clientKeep, tile.loc);

            int score = playerDistToVillage + clientDistToVillage;
            if(bestResult == null || score < bestScore) {
                bestResult = tile;
                bestScore = score;
            }
        }

        return bestResult;
    }

    public override bool IsEligible(Team clientTeam)
    {
        foreach(TeamInfo t in GameController.instance.gameState.teams) {
            //only allow one quest like this.
            if(t.enemyOfPlayer == false && t.currentQuest != null && t.currentQuest.quest == this) {
                return false;
            }
        }

        return FindTargetVillage(clientTeam) != null;
    }

    public override float ScoreQuest(QuestInProgress info)
    {
        return 100.0f;
    }

    public override string GetDetails(QuestInProgress info)
    {
        return "Gain experience for your leader and level them up to level 3";
    }

    public override string GetAspiration(QuestInProgress info)
    {
        return "I will gain experience";
    }

    public override string AchievementText(QuestInProgress info)
    {
        return "Proving yourself a worty leaer";
    }

    public override bool Completed(QuestInProgress questInProgress)
    {
        return GameController.instance.gameState.GetOwnerOfLoc(questInProgress.itemRetrieveLoc) == GameController.instance.playerTeamInfo.nteam;
    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.count = 0;
        questInProgress.countNeeded = 1;
        questInProgress.itemRetrieveLoc = FindTargetVillage(clientTeam).loc;
    }

    public override void OnRequestQuest(QuestInProgress questInProgress)
    {
        Tile tile = GameController.instance.map.GetTile(questInProgress.itemRetrieveLoc);
        if(tile != null && tile.unit == null) {
            GameController.instance.ForceCaptureLoc(tile.loc, GameController.instance.primaryEnemyTeamInfo.nteam);
            foreach(UnitType unitType in summonUnits) {
                Loc summonLoc = GameController.instance.FindVacantTileNear(tile.loc, unitType.unitInfo);
                GameController.instance.ExecuteRecruit(new RecruitCommandInfo() {
                    unitType = unitType,
                    loc = summonLoc,
                    seed = 0,
                    team = GameController.instance.primaryEnemyTeamInfo.team,
                    unitAssignment = AIUnitAssignment.StaticGuard,
                });
            }
        }
    }

    public override void OnUnitLeveled(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.team.player && unit.unitInfo.ruler) {
            questInProgress.count = unit.unitInfo.level;
        }
    }

    public override string QuestHint(QuestInProgress questInProgress)
    {
        return "they value leaders who are willing to lead their troops into battle personally. They won't serve anyone who doesn't have a decent amount of experience on the battlefield.";
    }

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        return checkinNode;
    }

    public override bool AlmostFailed(QuestInProgress questInProgress)
    {
        return false;
    }

    public override bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        return false;
    }

}
