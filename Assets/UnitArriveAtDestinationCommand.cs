using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitArriveAtDestinationCommand : GameCommand
{
    public string unitGuid;

    public override bool RunImmediately()
    {
        GameController.instance.UnitArriveAtDestination(GameController.instance.GetUnitByGuid(unitGuid));
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
