using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/ExitUnderworld")]
public class BoonExitUnderworld : Boon
{
    List<Loc> FindLocs(Unit unit)
    {
        List<Loc> result = new List<Loc>();
        foreach(Tile tile in unit.tile.adjacentTiles) {
            if(tile == null) {
                continue;
            }
            Tile aboveTile = GameController.instance.map.GetTile(tile.loc.toOverworld);
            if(aboveTile.unit != null || aboveTile.loot != null || aboveTile.terrain.rules.village || aboveTile.terrain.rules.castle || aboveTile.terrain.rules.keep || aboveTile.terrain.rules.aquatic) {
                continue;
            }

            if(tile.loot == null && tile.loc.underworld && tile.unit == null && tile.terrain.rules.village == false && tile.terrain.rules.castle == false && tile.terrain.rules.keep == false) {
                result.Add(tile.loc);
            }
        }

        return result;
    }

    public override bool IsEligible(Unit unit)
    {
        if(FindLocs(unit).Count == 0) {
            Debug.Log("UNDERWORLD BOON NOT ELIGIBLE NO LOCS");

            return false;
        }

        //see if we can get to the overworld easily enough from this location.
        var paths = Pathfind.FindPaths(GameController.instance, unit.unitInfo, 8, new Pathfind.PathOptions() {
            ignoreZocs = true,
            moveThroughEnemies = true,
        });

        foreach(var p in paths) {
            if(p.Key.overworld) {
                Debug.Log("UNDERWORLD BOON NOT ELIGIBLE TOO CLOSE");
                return false;
            }
        }

        Debug.Log("UNDERWORLD BOON ELIGIBLE");

        return true;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);

        List<Loc> choices = FindLocs(unit);
        int highestCost = -1;
        Loc choice = Loc.invalid;

        foreach(Loc loc in choices) {
            Tile t = GameController.instance.map.GetTile(loc);
            int cost = unit.unitInfo.MoveCost(t);
            if(cost > highestCost) {
                choice = loc;
                highestCost = cost;
            }
        }

        Tile gate = GameController.instance.map.GetTile(choice.toOverworld);
        gate.underworldGate = true;

        Tile underworldTile = GameController.instance.map.GetTile(choice.toUnderworld);
        underworldTile.isvoid = true;
        underworldTile.gameObject.SetActive(false);

        GameController.instance.underworldMap.SetupEdges();
        GameController.instance.map.SetupEdges();

        unit.unitInfo.expendedVision = false;
        GameController.instance.RecalculateVision();

        GameController.instance.ShowDialogMessage(new ConversationDialog.Info() {
            title = "Daylight!",
            text = "The villagers helpfully show you a secret passage that allows for an alternative exit to the cavern you are in!",
        }, new ConversationDialog.Result());
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
