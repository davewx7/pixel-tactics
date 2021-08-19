using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Mining")]
public class BoonMining : Boon
{
    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);

        GameController.instance.ShowDialogMessage(new ConversationDialog.Info() {
            title = "Mine Re-opened",
            text = "Re-opening the mine increases your gold income!",
        }, new ConversationDialog.Result());
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
