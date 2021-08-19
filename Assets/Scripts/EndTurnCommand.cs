using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTurnCommand : GameCommand
{
    public override bool RunImmediately()
    {
        GameController.instance.DoEndTurn();
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
