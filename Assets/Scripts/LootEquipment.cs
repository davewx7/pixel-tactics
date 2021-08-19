using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(menuName = "Wesnoth/LootEquipment")]
public class LootEquipment : Loot
{
    public int tier = 1;

    List<Equipment> GetEquipmentCandidates(Unit claimingUnit, LootItem lootItem)
    {
        List<Equipment> result = new List<Equipment>();
        List<Equipment> resultNonEquippable = new List<Equipment>();

        foreach(Equipment equip in Equipment.all) {
            if(equip.tier != tier || GameController.instance.gameState.equipmentAwarded.Contains(equip)) {
                continue;
            }

            if(equip.EquippableForUnit(claimingUnit.unitInfo) == false) {
                resultNonEquippable.Add(equip);
                continue;
            }

            result.Add(equip);
        }

        if(result.Count == 0) {
            return resultNonEquippable;
        }

        return result;
    }

    public override void GetLoot(Unit claimingUnit, LootItem lootItem)
    {
        var candidates = GetEquipmentCandidates(claimingUnit, lootItem);

        if(candidates.Count == 0) {
            base.GetLoot(claimingUnit, lootItem);
            return;
        }

        var equip = candidates[GameController.instance.rng.Range(0, candidates.Count)];

        bool canEquip = equip.EquippableForUnit(claimingUnit.unitInfo);

        if(canEquip) {
            GameController.instance.ExecuteGrantEquipment(claimingUnit, equip);
        } else {
            claimingUnit.teamInfo.equipmentStored.Add(equip);
        }

        GameController.instance.gameState.equipmentAwarded.Add(equip);

        string equipMessage = "";
        if(canEquip == false) {
            equipMessage = " This item could not be equipped, so it has been placed in the convoy.";
        }

        GameController.instance.ShowDialogMessage(equip.description, string.Format("You have found <color=#ffffff>{0}</color>.{1}", equip.descriptionAsArticle, equipMessage));

        if(lootItem.lootInfo != null) {
            lootItem.lootInfo.equipment.Clear();
        }

        lootItem.gameObject.SetActive(false);
    }

}
