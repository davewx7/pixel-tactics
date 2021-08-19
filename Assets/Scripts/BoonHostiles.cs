using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Hostiles")]
public class BoonHostiles : Boon
{
    [SerializeField]
    List<UnitType> unitTypes = null;

    public override bool AllowOptions(Unit unit)
    {
        return false;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        List<Loc> locs = new List<Loc>();
        foreach(UnitType unitType in unitTypes) {
            Loc spawnLoc = GameController.instance.FindVacantTileNear(unit.loc, null, locs);
            if(GameController.instance.map.LocOnBoard(spawnLoc) == false) {
                continue;
            }

            UnitInfo unitInfo = unitType.createUnit();
            unitInfo.nteam = GameController.instance.gameState.numBarbarianTeam;
            unitInfo.loc = spawnLoc;
            GameController.instance.ExecuteSpawnUnit(unitInfo);

            locs.Add(spawnLoc);
        }
    }


}
