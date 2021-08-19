using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OfferFealtyInfo
{
    public Team team;
}

public class OfferFealtyCommand : GameCommand
{
    public OfferFealtyInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<OfferFealtyInfo>(data); }


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
