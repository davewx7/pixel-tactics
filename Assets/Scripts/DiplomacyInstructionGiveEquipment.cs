using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/DiplomacyInstruction/EquipmentGift")]
public class DiplomacyInstructionGiveEquipment : DiplomacyInstruction
{
    public int tier = 1;

    public override void Execute(DiplomacyNodeInfo info)
    {
        var candidates = GetCandidates(info.playerUnit, info.aiUnit);
        if(candidates.Count > 0) {
            var equip = candidates[GameController.instance.rng.Next(candidates.Count)];
            info.playerUnit.GiveUnitEquipment(equip);
        }
    }

    public List<Equipment> GetCandidates(Unit unit, Unit aiUnit)
    {
        List<Equipment> result = new List<Equipment>();
        foreach(Equipment equip in Equipment.all) {
            if(equip.tier == this.tier && unit.unitInfo.equipment.Contains(equip) == false && unit.teamInfo.equipmentInMarket.Contains(equip) == false) {
                result.Add(equip);
            }
        }

        return result;
    }
}
