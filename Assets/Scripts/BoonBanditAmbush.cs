using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/BanditAmbush")]
public class BoonBanditAmbush : BoonHostiles
{
    public override bool AllowOptions(Unit unit)
    {
        return true;
    }

    public override bool IsEligible(Unit unit)
    {
        if(GameController.instance.gameState.nround < 6) {
            return false;
        }

        if(unit.teamInfo.gold < 6) {
            //don't have enough gold to attract bandits.
            return false;
        }
        
        foreach(Unit u in GameController.instance.units) {
            //only ambush if this unit is scouting off on its own.
            if(u != unit && u.team.barbarian == false && Tile.DistanceBetween(u.loc, unit.loc) < 6) {
                return false;
            }
        }

        return true;
    }
}
