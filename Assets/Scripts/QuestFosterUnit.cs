using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/Foster")]
public class QuestFosterUnit : Quest
{
    public DiplomacyNode offerNode;

    public override bool IsEligible(Team clientTeam)
    {
        int count = 0;

        foreach(TeamInfo t in GameController.instance.gameState.teams) {
            if(t.enemyOfPlayer == false && t.currentQuest != null && t.currentQuest.quest == this) {
                ++count;
            }
        }

        if(count >= 2) {
            return false;
        }

        return true;
    }


    public override QuestType questType {
        get { return QuestType.FosterUnit; }
    }

    public override float ScoreQuest(QuestInProgress info)
    {
        float result = 100f;
        if(info.clientTeam.teamInfo.relationsWithPlayerRating < 40) {
            //if the ai has a poor relationship with the player this quest
            //should score more poorly.
            result -= (40f - info.clientTeam.teamInfo.relationsWithPlayerRating)*2f;
        }
        return result;
    }

    public override void OnAcceptQuest(QuestInProgress questInProgress)
    {

    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.count = 1;
        questInProgress.countNeeded = 2;
    }


    public override void OnUnitLeveled(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.unitInfo.guid == questInProgress.unitGuid) {
            questInProgress.count = unit.unitInfo.level;
        }
    }


    public override string GetDetails(QuestInProgress info)
    {
        return string.Format("Level {0} up to level {1}.", info.unitName, info.countNeeded);
    }

    public override string QuestHint(QuestInProgress questInProgress)
    {
        return string.Format("It is said that there is a young princeling among them that they wish had the chance to gain more experience in battle.");
    }

    public override string AchievementText(QuestInProgress info)
    {
        return "Fostering their kin";
    }


    public List<DiplomacyNode> checkinNodeProgress = new List<DiplomacyNode>();

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        if(questInProgress.count >= 2) {
            return checkinNodeProgress;
        }
        return base.GetCustomCheckin(questInProgress);
    }

    public override bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        if(questInProgress.GetUnit() == null) {
            return true;
        }

        return false;
    }
}
