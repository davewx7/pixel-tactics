using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/RefreshMovement")]
public class BoonRefreshMovement : Boon
{
    public override void Award(AwardBoonInfo info, Unit unit)
    {
        unit.unitInfo.movementExpended = 0;
        unit.unitInfo.expendedVision = false;

        GameController.instance.ShowDialogMessage("Refreshed", string.Format("The villagers supply you and keep you moving. Your <color=#ffffff>{0}</color>.", "movement is restored"));
    }
}
