using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(menuName = "Wesnoth/AggroAI")]
public class AggroAI : AI
{
    protected override bool IsTeamAnEnemy(AIState aiState, Team enemyTeam)
    {
        return enemyTeam != null && enemyTeam.barbarian == false && enemyTeam.primaryEnemy == false;
    }



    public override bool UnitThink(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        if(base.UnitThink(unit, paths)) { //take care of basic attacking.
            Debug.Log("Aggro return from attack");
            return true;
        }

        return UnitThinkGoAfterPlayer(unit, paths);
    }


}