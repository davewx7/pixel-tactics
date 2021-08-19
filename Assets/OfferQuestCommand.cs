using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfferQuestCommand : GameCommand
{
    public QuestInProgress quest;

    public override string Serialize() { return Glowwave.Json.ToJson(quest); }
    public override void Deserialize(string data) { quest = Glowwave.Json.FromJson<QuestInProgress>(data); }


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GameController.instance.ShowQuestOffer(this));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
