using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/CursedItem")]
public class BoonCursedItem : Boon
{
    public List<Equipment> GetCandidates(Unit unit)
    {
        List<Equipment> result = new List<Equipment>();
        foreach(Equipment equip in Equipment.all) {
            if(equip.cursed && equip.EquippableForUnit(unit.unitInfo) && unit.teamInfo.equipmentInMarket.Contains(equip) == false) {

                bool owned = false;

                //if another unit has this equipment don't allow it.
                foreach(Unit u in GameController.instance.units) {
                    if(u.unitInfo.equipment.Contains(equip)) {
                        owned = true;
                        break;
                    }
                }

                if(owned == false) {
                    result.Add(equip);
                }
            }
        }

        return result;
    }

    public override bool IsEligible(Unit unit)
    {

        if(GameController.instance.gameState.GetLastBoonOfferRound(this) >= 0) {
            //only offer this boon once per game.
            return false;
        }

        return GetCandidates(unit).Count > 0;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        ConsistentRandom rng = new ConsistentRandom(info.seed);
        var candidates = GetCandidates(unit);
        var equip = candidates[rng.Next()%candidates.Count];
        unit.unitInfo.equipment.Add(equip);

        string text = string.Format("You open the box, finding a <color=#ff0000><link=\"equip\">{0}</link></color> within. With a shudder you feel it bind itself to you. <i>\"It is yours now!\"</i> the figure pronounces with a cackle, running off down the street.", equip.description);

        string tooltipText = equip.FullTooltip();

        GameController.instance.ShowDialogMessage(new ConversationDialog.Info() {
            title = "Cursed Equipment",
            text = text,
        }.AddLink("equip",
            new TooltipText.Options() {
                text = tooltipText,
                icon = equip.icon,
                iconSize = new Vector2(64f, 64f),
                iconMaterial = equip.CreateMaterial(),
                useMousePosition = true,
            }
        ));
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
