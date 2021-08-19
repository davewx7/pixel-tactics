using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/HideUnit")]
public class BoonHideUnit : Boon
{
    [SerializeField]
    UnitMod _mod = null;
    
    public override bool IsEligible(Unit unit)
    {
        if(unit.unitInfo.hitpointsRemaining >= unit.unitInfo.hitpointsMax/3) {
            //only give the option of hiding if a unit's hitpoints are low.
            return false;
        }

        int lastOffered = GameController.instance.gameState.GetLastBoonOfferRound(this);

        if(lastOffered >= 0 && lastOffered > GameController.instance.gameState.nround-3) {
            return false;
        }

        //have to be an enemy within two spaces or at least two enemies within four spaces.
        int points = 0;
        List<Loc> locs = Tile.GetTilesInRadius(unit.loc, 4);
        foreach(Loc loc in locs) {
            Unit enemyUnit = GameController.instance.GetUnitAtLoc(loc);
            if(enemyUnit != null && enemyUnit.IsEnemy(unit)) {
                if(Tile.DistanceBetween(loc, unit.loc) <= 2) {
                    points += 2;
                } else {
                    ++points;
                }

                if(points >= 2) {
                    break;
                }
            }
        }

        return points >= 2;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        unit.unitInfo.AddModUntilEndOfTurn(_mod);
        unit.RefreshStatusDisplay();

        GameController.instance.ShowDialogMessage("Invisible!", "Concealing yourself in the hay, you are <color=#ffffff>Invisible</color> this round.");
    }
}
