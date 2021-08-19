using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/LevelUpLeader")]
public class QuestLevelUpLeader : Quest
{
    public DiplomacyNode offerNode;

    public override QuestType questType {
        get { return QuestType.LevelUpRuler; }
    }

    public override bool IsEligible(Team clientTeam)
    {
        foreach(TeamInfo t in GameController.instance.gameState.teams) {
            //only allow one quest like this.
            if(t.enemyOfPlayer == false && t.currentQuest != null && t.currentQuest.quest == this) {
                return false;
            }
        }

        var playerUnit = GameController.instance.playerTeamInfo.GetRuler();
        if(playerUnit == null) {
            return false;
        }

        return playerUnit.unitInfo.level <= 1;
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


    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.count = 1;
        questInProgress.countNeeded = 3;
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
        if(questInProgress.timeUntilExpired > 0) {
            return false;
        }

        var playerUnit = GameController.instance.playerTeamInfo.GetRuler();
        if(playerUnit == null || playerUnit.unitInfo.level >= 3) {
            return false;
        }

        if(playerUnit.unitInfo.level == 2) {
            return questInProgress.timeUntilExpired < -3;
        } else {
            return questInProgress.timeUntilExpired <= 0;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
