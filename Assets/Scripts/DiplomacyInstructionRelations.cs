using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/DiplomacyInstruction/Relations")]
public class DiplomacyInstructionRelations : DiplomacyInstruction
{
    [SerializeField]
    Team.DiplomacyStatus _status = Team.DiplomacyStatus.Hostile;

    public override void Execute(DiplomacyNodeInfo info)
    {
        info.aiUnit.teamInfo.SetRelations(_status);
    }
}
