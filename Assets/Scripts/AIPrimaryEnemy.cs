using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPrimaryEnemy : AI
{
    [System.Serializable]
    public class LaunchAttack
    {
        public int nround;
        public int unitCost;
    }

    public List<LaunchAttack> attackSchedule;

    public override void RemoveForbiddenVillagesFromPaths(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        //no villages are forbidden to us, we take whatever we want.
    }

    public override void WarWithPlayerStarted(AIState aiState)
    {

    }

    override protected bool IsTeamAnEnemy(AIState state, Team enemyTeam)
    {
        return enemyTeam != null && enemyTeam.playerOrAllyOfPlayer;
    }


    public override void NewTurn(AIState aiState)
    {
        base.NewTurn(aiState);

        //Asheviere's units that aren't revealed to the player get free
        //experience to make them slowly more powerful over time.
        foreach(Unit unit in aiState.teamInfo.GetUnits()) {
            if(unit.unitInfo.ruler == false && unit.tile.fogged) {
                unit.unitInfo.experience += GameController.instance.rng.Range(0, 4);
            }
        }

        int nround = GameController.instance.gameState.nround;
        foreach(LaunchAttack attack in attackSchedule) {
            if(attack.nround == nround) {
                DoLaunchAttack(attack);
            }
        }
    }

    public void DoLaunchAttack(LaunchAttack attack)
    {
        List<Unit> potentials = new List<Unit>();
        foreach(Unit unit in _aiState.teamInfo.GetUnits()) {
            if(unit.unitInfo.ruler == false && _aiState.GetUnitOrders(unit).assignment == AIUnitAssignment.HomeGuard) {
                potentials.Add(unit);
            }
        }

        int gold = attack.unitCost;
        while(gold > 0 && potentials.Count > 0) {
            int index = GameController.instance.rng.Next(potentials.Count);
            Unit u = potentials[index];
            Debug.LogFormat("ATTACK PLAYER: {0} {1} {2} gold left {3} units left", u.unitInfo.unitType.classDescription, u.loc.ToString(), gold, potentials.Count);
            _aiState.SetUnitOrders(new AIUnitOrders() { unitGuid = u.unitInfo.guid, assignment = AIUnitAssignment.AttackPlayer });
            gold -= u.unitInfo.unitType.cost;
            potentials.RemoveAt(index);
        }
    }
}
