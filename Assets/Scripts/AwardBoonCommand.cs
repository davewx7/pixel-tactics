using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AwardBoonInfo
{
    public string unitGuid;
    public Boon boon;
    public bool interactable = true;
    public int seed;
    public List<Boon> choices = new List<Boon>();
}

public class AwardBoonCommand : GameCommand
{
    public AwardBoonInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<AwardBoonInfo>(data); }


    // Start is called before the first frame update
    void Start()
    {
        Debug.LogFormat("Award boon command!!!");
        var unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        info.boon.Award(info, unit);

        foreach(Boon choice in info.choices) {
            choice.RecordOffer(info.seed, unit, choice == info.boon);
            GameController.instance.gameState.RecordBoonOffer(choice);
        }

        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
