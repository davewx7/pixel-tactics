using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Peasants")]
public class BoonPeasants : Boon
{
    [SerializeField]
    UnitType _spawnType = null;

    [SerializeField]
    int _spawnQuantity = 3;

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        List<Loc> locs = new List<Loc>();
        for(int i = 0; i < _spawnQuantity; ++i) {
            Loc spawnLoc = GameController.instance.FindVacantTileNear(unit.loc, null, locs);
            if(spawnLoc.valid) {
                GameController.instance.ExecuteRecruit(new RecruitCommandInfo() {
                    unitType = _spawnType,
                    loc = spawnLoc,
                    summonerGuid = unit.summonerOrSelf.unitInfo.guid,
                    unitStatus = new List<UnitStatus>() { GameConfig.instance.statusTemporal },
                    haveHaste = true,
                });

                locs.Add(spawnLoc);
            }
        }
    }
}
