using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using DG.Tweening;

[System.Serializable]
public class GrantEquipmentInfo
{
    public string unitGuid;
    public List<Equipment> equipment = new List<Equipment>();
}


public class GrantEquipmentCommand : GameCommand
{
    public GrantEquipmentInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<GrantEquipmentInfo>(data); }

    IEnumerator Execute()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        Assert.IsNotNull(unit);

        if(unit == null) {
            finished = true;
            yield break;
        }


        foreach(Equipment equip in info.equipment) {
            unit.GiveUnitEquipment(equip);

            UnitStatusPanel panel = GameController.instance.statusPanel;
            panel.Init(unit);
            panel.locked = true;

            InventorySlotDisplay slot = panel.GetInventorySlot(equip);

            Assert.IsNotNull(slot);

            if(slot != null) {
                slot.AnimateGetEquipment();
                yield return new WaitForSeconds(1f);
            }

            panel.locked = false;
        }


        finished = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Execute());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
