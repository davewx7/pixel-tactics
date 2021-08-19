using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CreateBuildingCommandInfo
{
    public EconomyBuilding building = null;
}



public class CreateBuildingCommand : GameCommand
{
    public CreateBuildingCommandInfo info = new CreateBuildingCommandInfo();

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<CreateBuildingCommandInfo>(data); }

    // Start is called before the first frame update
    void Start()
    {
        GameController.instance.CreateBuildingCmd(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
