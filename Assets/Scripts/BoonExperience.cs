using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Experience")]
public class BoonExperience : Boon
{
    public override bool IsEligible(Unit unit)
    {
        return unit.unitInfo.level <= 1 && unit.unitInfo.experience < 10;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);

        unit.unitInfo.GainExperience(5);

        GameController.instance.ShowDialogMessage("Training", string.Format("The villagers organize a tourney to train in. By fighting in it, {0} gains <color=#ffffff>5 experience</color>! Excited by the tournament, the villagers build a <color=#ffffff>Practice Yard</color> in the village. Any unit that completes a rest in this village will gain 5 experience.", string.IsNullOrEmpty(unit.unitInfo.characterName) ? unit.unitInfo.unitType.classDescription : unit.unitInfo.characterName));
    }
}
