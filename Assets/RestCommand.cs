using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RestCommandInfo
{
    public string guid;
}



public class RestCommand : GameCommand
{
    public RestCommandInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<RestCommandInfo>(data); }

    // Start is called before the first frame update
    void Start()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.guid);

        if(unit.canCancelRest) {
            unit.unitInfo.status.Remove(GameConfig.instance.statusFallingAsleep);
            unit.unitInfo.Unexhaust();
            unit.unitInfo.resting = false;
            unit.RefreshStatusDisplay();
            GameController.instance.RefreshUnitDisplayed();
            finished = true;
            return;
        }

        unit.ApplyStatus(GameConfig.instance.statusFallingAsleep);
        unit.unitInfo.Exhaust();
        unit.unitInfo.resting = true;
        GameController.instance.RefreshUnitDisplayed();
        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
