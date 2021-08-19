using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/VillageBuilding")]
public class BoonVillageBuilding : Boon
{
    public override string GetTooltipText(Unit unit)
    {
        return string.Format("<color=#ffffff>{0}</color><color=#aaaaaa>: {1}", createBuilding.description, createBuilding.rulesText);
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);
        GameController.instance.ShowDialogMessage(createBuilding.description, string.Format("This village now has a <color=#ffffff>{0}</color> in it. {1}", createBuilding.description, createBuilding.rulesText));
    }

}
