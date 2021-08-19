using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Blessing")]
public class BoonBlessing : Boon
{
    public List<UnitStatus> blessings;
    public override bool IsEligible(Unit unit)
    {
        //Can only get a blessing if the unit doesn't have one.
        return unit.unitInfo.blessed == false;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);

        int index = info.seed%blessings.Count;
        var item = blessings[index];
        unit.ApplyStatus(item);

        GameController.instance.ShowDialogMessage("Blessing", string.Format("You pray at the village's shrine, and feel a <color=#ffffff>{0}</color> come upon you. This village has a <color=#ffffff>Shrine</color> in it. Units that rest here will receive a blessing.", item.description));
    }
}
