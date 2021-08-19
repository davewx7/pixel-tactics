using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Curse")]
public class BoonCurse : Boon
{
    public string curseMessage;
    public UnitStatus curse;
    public int gold;

    public override bool IsEligible(Unit unit)
    {
        //Can only get a curse if the unit doesn't have one.
        return unit.unitInfo.cursed == false;
    }

    public override void Award(AwardBoonInfo boonInfo, Unit unit)
    {
        base.Award(boonInfo, unit);

        unit.ApplyStatus(curse);
        unit.teamInfo.EarnGold(gold);

        ConversationDialog.Info info = new ConversationDialog.Info() {
            title = "Curse",
            text = curseMessage,
        };

        info.AddLink("curse", new TooltipText.Options() {
            icon = curse.icon,
            text = "Curse (status effect): " + curse.tooltip,
            useMousePosition = true,
        });

        GameController.instance.ShowDialogMessage(info);
    }
}
