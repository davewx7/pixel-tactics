using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Potion")]
public class BoonPotion : Boon
{
    public List<Equipment> EligiblePotions(Unit unit) {
            var result = new List<Equipment>();
            foreach(Equipment equip in Equipment.all) {
                if(equip.consumable && equip.EquippableForUnit(unit.unitInfo)) {
                    result.Add(equip);
                }
            }

            return result;
    }

    public override bool IsEligible(Unit unit)
    {
        return EligiblePotions(unit).Count > 0;
    }


    public override void Award(AwardBoonInfo info, Unit unit)
    {
        var potions = EligiblePotions(unit);

        ConsistentRandom rng = new ConsistentRandom(info.seed);
        int index = rng.Range(0, potions.Count);

        string itemName = "";
        var potion = potions[index];
        itemName = potion.description;

        GameController.instance.ExecuteGrantEquipment(unit, potion);

        GameController.instance.ShowDialogMessage(new ConversationDialog.Info() {
            title = "Item",
            text = string.Format("The villagers find you a <color=#aaaaff><link=\"equip\">{0}</link></color> to aid you.", itemName),
        }.AddLink("equip", potion.CreateTooltip()));
    }
}
