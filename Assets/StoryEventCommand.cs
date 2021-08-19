using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryEventCommand : GameCommand
{
    public StoryEventInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<StoryEventInfo>(data); }

    IEnumerator ExecuteCo()
    {
        foreach(var terrainChange in info.terrainChanges) {
            Tile t = GameController.instance.map.GetTile(terrainChange.loc);
            HashSet<Tile> tilesUpdate = new HashSet<Tile>();
            if(t != null) {
                t.terrain = terrainChange.terrain;

                tilesUpdate.Add(t);
                foreach(Tile adj in t.adjacentTiles) {
                    if(adj != null) {
                        tilesUpdate.Add(adj);
                    }
                }
            }

            foreach(Tile tile in tilesUpdate) {
                tile.CalculatePosition();
            }
        }

        foreach(var dialogItem in info.dialog) {
            GameController.instance.ShowDialogMessage(dialogItem.dialogTitle, dialogItem.dialogText, null);
            yield return new WaitUntil(() => GameConfig.modalDialog == 0);
        }

        GameController.instance.playerTeamInfo.gold += info.goldReward;
        GameController.instance.playerTeamInfo.AddStoryScore(info.renownDescription, info.renownReward);

        finished = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ExecuteCo());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
