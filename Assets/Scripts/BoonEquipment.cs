using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Equipment")]
public class BoonEquipment : Boon
{
    public List<Equipment> GetCandidates(Unit unit) {

        List<Equipment> result = new List<Equipment>();
        foreach(Equipment equip in Equipment.all) {            
            if(equip.tier >= 0 && equip.tier <= 2 && equip.price <= unit.teamInfo.gold && equip.EquippableForUnit(unit.unitInfo) && unit.teamInfo.equipmentInMarket.Contains(equip) == false) {
                result.Add(equip);
            }
        }

        return result;
    }

    public override bool IsEligible(Unit unit)
    {
        return GetCandidates(unit).Count > 5 && GameController.instance.gameState.nround > 0;
    }


    public override void Award(AwardBoonInfo info, Unit unit)
    {
        ConsistentRandom rng = new ConsistentRandom(info.seed);
        var equipment = GetCandidates(unit);

        while(equipment.Count > 5) {
            equipment.RemoveAt(rng.Range(0, equipment.Count));
        }

        TeamInfo.MarketInfo market = new TeamInfo.MarketInfo() {
            equipment = equipment,
            priceMultiplier = 70,
        };

        unit.teamInfo.AddTemporaryMarket(unit.loc, market);

        if(info.interactable) {
            GenericCommandInfo cmd = GameController.instance.QueueGenericCommand();
            AIDiplomacyManager.instance.StartCoroutine(AIDiplomacyManager.instance.VillageMarket(unit, cmd));
        }
    }
}
