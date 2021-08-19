using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/Bounty")]
public class QuestBounty : Quest
{
    [TextArea(3,3)]
    public string preludeText;

    public override string GetDetailsPrelude(QuestInProgress info)
    {
        return preludeText;
    }

    public override string GetDetails(QuestInProgress info)
    {
        return string.Format("We are hoping that you can prove your worth by killing {0} {1}. If you can do that we shall be so impressed that we will support your claim to the Throne, such that it is.", info.countNeeded, info.unitTags[0].descriptionPlural);
    }

    public override string GetSummary(QuestInProgress info)
    {
        string villageName = GameController.instance.map.GetTile(GetQuestTarget(info)).GetLabelText();
        return string.Format("Killed {0}/{1} {2}", info.count, info.countNeeded, info.unitTags[0].descriptionPlural);
    }


    public override void OnEnemyUnitKilled(Unit unit, QuestInProgress questInProgress)
    {
        bool matchesCriteria = false;
        foreach(UnitTag tag in unit.unitInfo.unitType.tags) {
            if(questInProgress.unitTags.Contains(tag)) {
                matchesCriteria = true;
                break;
            }
        }

        if(matchesCriteria) {
            questInProgress.count++;
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
