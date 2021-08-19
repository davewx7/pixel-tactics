using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpeechPromptInfo
{
    public SpeechPromptInstance prompt;
    public string unitGuid;
}

public class SpeechPromptCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<SpeechPromptInfo>(data); }

    public SpeechPromptInfo info;

    IEnumerator Execute()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        if(unit != null) {
            ScrollCameraCommand.CameraLookAt(unit.loc);

            yield return new WaitForSeconds(0.2f);

            GameController.instance.speechQueue.Enqueue(new SpeechPromptQueue.Item() {
                unit = unit,
                instance = info.prompt.Clone(),
            });

            while(GameController.instance.speechQueue.unitCurrentlySpeaking) {
                yield return null;
            }
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
