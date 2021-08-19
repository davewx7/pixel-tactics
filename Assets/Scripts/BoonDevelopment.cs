using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Development")]
public class BoonDevelopment : Boon
{
    public int amount = 20;

    public string resultTitle = "Caring for your people";
    public string resultText = "The villagers are impressed by your compassion even in times of difficulty. Your renown through the realm grows.";

    public override bool IsEligible(Unit unit)
    {
        return base.IsEligible(unit);
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        GameController.instance.ShowDialogMessage(resultTitle, resultText);
        unit.teamInfo.scoreInfo.kindness += amount;
    }
}
