using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[System.Serializable]
public class DiscardEquipmentInfo
{
    public string unitGuid;
    public Equipment equipment;
    public Loc target;
}

public class DiscardEquipmentCommand : GameCommand
{
    public DiscardEquipmentInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<DiscardEquipmentInfo>(data); }

    // Start is called before the first frame update
    void Start()
    {
        finished = true;

        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        Assert.IsNotNull(unit);
        if(unit != null) {
            unit.unitInfo.equipment.Remove(info.equipment);

            if(info.target.valid) {
                Unit targetUnit = GameController.instance.GetUnitAtLoc(info.target);
                if(targetUnit != null) {
                    targetUnit.unitInfo.equipment.Add(info.equipment);
                } else {
                    Tile t = GameController.instance.map.GetTile(info.target);
                    if(t != null) {
                        t.AddLoot(GameConfig.instance.deadBodyLoot, new LootInfo() {
                            description = unit.unitInfo.characterName,
                            equipment = new List<Equipment>() { info.equipment },
                        });
                    }
                }
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
