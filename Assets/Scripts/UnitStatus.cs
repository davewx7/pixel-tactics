using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/UnitStatus")]
public class UnitStatus : GWScriptableObject
{
    public Sprite icon;
    public string description;
    public string tooltip;

    public string GetTooltip(Unit unit)
    {
        if(unit != null && this == GameConfig.instance.statusTemporal) {
            int expireRound = unit.unitInfo.roundCreated + unit.teamInfo.temporalUnitDuration;
            return string.Format("This temporal unit will be disbanded after {0} more moons.\nTemporal units exist for {1} moons in total.", expireRound - GameController.instance.gameState.nround, unit.teamInfo.temporalUnitDuration);
        }
        return tooltip;
    }

    public string applySlogan;

    public bool expiresAfterOneRound = true;
    public bool displayIconOnUnit = true;

    public UnitMod unitMod = new UnitMod();
}
