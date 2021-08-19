using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Gold")]
public class BoonGold : Boon
{
    public string messageText = "The villagers scrounge together <color=#ffffff>6 gold</color> to offer you as tribute.";
    public int amount = 6;
    public override string GetEffectText(Unit unit, int nseed)
    {
        if(amount > 0) {
            return string.Format("Gain {0} gold", amount);
        } else {
            return string.Format("Lose {0} gold", -amount);
        }
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        unit.teamInfo.EarnGold(amount);

        GameController.instance.ShowDialogMessage("Gold", messageText);
    }
}
