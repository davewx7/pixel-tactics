using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/CreateWar")]
public class BoonCreateWar : Boon
{
    public int reward = 25;
    public List<TeamInfo> GetEligibleWars(Unit unit)
    {
        List<TeamInfo> result = new List<TeamInfo>();

        foreach(TeamInfo team in GameController.instance.teams) {
            if(team.team.regularAITeam && team.hasPlayerContact && team.enemyOfPlayer == false && team.allyOfPlayer == false) {
                result.Add(team);
            }
        }

        return result;
    }

    public TeamInfo GetWar(Unit unit, int nseed)
    {
        var options = GetEligibleWars(unit);
        return options[nseed%options.Count];
    }

    public override string GetDialogStoryline(Unit unit, int nseed)
    {
        string storyText = "<i>\"Good morrow to you! I represent the Western Merchant's Guild, and we have been tracking your movements for quite some time. You see, the Merchant's Guild tries to act in the best interests of trade and commerce throughout the land. Unfortunately, {0} are impeding this. We are therefore in the process of teaching them the value of coin. We would like to offer you a nice generous loan in exchange for you raising your banners against them.\"</i>";
        return string.Format(storyText, GetWar(unit,nseed).team.teamNameAsProperNoun, reward);
    }

    public override string GetEffectText(Unit unit, int nseed)
    {
        string teamName = GetWar(unit, nseed).team.teamNameAsProperNoun;
        return string.Format("Declare war on {0}, gain {1} gold", teamName, reward);
    }

    public override bool IsEligible(Unit unit)
    {
        return GetEligibleWars(unit).Count > 0;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        var t = GetWar(unit, info.seed);
        unit.teamInfo.gold += reward;
        t.StartWarWithPlayer();

        GameController.instance.ShowDialogMessage("A Loan Taken", string.Format("The loan terms are generous and you receive a nice large purse full of gold. However, you realize that you are now at war with {0} and wonder if the cost will be worth it.", t.team.teamNameAsProperNoun));
    }
}
