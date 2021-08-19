using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/DiplomacyInstruction/AcceptQuest")]
public class DiplomacyInstructionAcceptQuest : DiplomacyInstruction
{
    public override void Execute(DiplomacyNodeInfo info)
    {
        TeamInfo team = info.aiUnit.teamInfo;
        if(team.currentQuest != null) {
            team.currentQuest.quest.OnAcceptQuest(team.currentQuest);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
