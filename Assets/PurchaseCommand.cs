using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PurchaseInfo
{
    public string unitGuid;
    public List<Equipment> equipment = new List<Equipment>();
    public List<Equipment> storeEquipment = new List<Equipment>();
    public List<Equipment> removeEquipment = new List<Equipment>();
    public List<Equipment> quaffEquipment = new List<Equipment>();

    public int cost;
}

public class PurchaseCommand : GameCommand
{
    public PurchaseInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<PurchaseInfo>(data); }


    // Start is called before the first frame update
    void Start()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        if(unit != null) {
            unit.teamInfo.gold -= info.cost;

            unit.unitInfo.equipment = info.equipment;

            foreach(var equip in info.storeEquipment) {
                unit.teamInfo.equipmentStored.Add(equip);
            }

            foreach(var equip in info.removeEquipment) {
                unit.teamInfo.equipmentStored.Remove(equip);
            }

            foreach(var equip in info.quaffEquipment) {
                unit.unitInfo.tired = true;
                equip.activatedAbility.CompleteCasting(unit, unit.loc);
            }
        }

        GameController.instance.RefreshUnitDisplayed();

        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
