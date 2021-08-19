using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/Exclave")]
public class QuestExclave : Quest
{
    Loc GetExclaveLoc(Team clientTeam)
    {
        TeamInfo teamInfo = GameController.instance.gameState.GetTeamInfo(clientTeam);
        AIState aiState = GameController.instance.aiStates[GameController.instance.teams.IndexOf(teamInfo)];
        if(aiState.exclaves.Count > 0) {
            return aiState.exclaves[0];
        }

        return Loc.invalid;
    }

    public override bool IsEligible(Team clientTeam)
    {
        Loc loc = GetExclaveLoc(clientTeam);
        if(loc.valid) {
            //quest is available if the player has never captured this village.
            if((GameController.instance.gameState.GetLocOwnerInfo(loc).pastOwnersBitmap&1) == 0) {
                return true;
            }
        }

        return false;
    }

    public override string GetDetailsPrelude(QuestInProgress info)
    {
        string villageName = GameController.instance.map.GetTile(GetQuestTarget(info)).GetLabelText();
        return string.Format("Greetings. Have you heard of the village of <color=#ffffff>{0}</color>? It was founded by the {1} long ago. I shall show it to you on a map...", villageName, info.clientTeam.teamName);
    }

    public override Loc GetQuestTarget(QuestInProgress info)
    {
        return GetExclaveLoc(info.clientTeam);
    }

    public override int GetQuestReveal(QuestInProgress info)
    {
        return 1;
    }

    public override string GetDetails(QuestInProgress info)
    {
        string villageName = GameController.instance.map.GetTile(GetQuestTarget(info)).GetLabelText();
        return string.Format("We have a great boon to ask of you: that you liberate <color=#ffffff>{0}</color> from the hands of our enemies. If you can do this you will prove yourself the rightful ruler of Wesnoth in our eyes.", villageName);
    }

    public override string GetSummary(QuestInProgress info)
    {
        string villageName = GameController.instance.map.GetTile(GetQuestTarget(info)).GetLabelText();
        return string.Format("Liberate the village of <color=#ffffff>{0}</color>", villageName);
    }

    public override string AchievementText(QuestInProgress info)
    {
        string villageName = GameController.instance.map.GetTile(GetQuestTarget(info)).GetLabelText();
        return "Liberation of " + villageName;
    }


    public override void OnUnitArrivesAtLoc(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.team.player && unit.tile.loc == GetQuestTarget(questInProgress)) {
            questInProgress.count++;
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
