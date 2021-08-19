using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CreateVillageBuildingCommandInfo
{
    public string unitGuid;
    public VillageBuilding building;
}

public class CreateVillageBuildingCommand : GameCommand
{
    public CreateVillageBuildingCommandInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<CreateVillageBuildingCommandInfo>(data); }


    // Start is called before the first frame update
    void Start()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        GameController.instance.gameState.SetVillageBuilding(unit.loc, info.building, false);
        unit.teamInfo.gold -= info.building.goldCost;
        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
