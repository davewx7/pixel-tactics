using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/TerrainRules")]
public class TerrainRules : GWScriptableObject
{
    public string terrainName;

    public int moveCost = 1;
    public int visionCost = 1;
    public int roadBuildCost = 100;
    public bool canReplaceWithRoad = false;

    public bool canLongRest = false;

    public bool capturable {
        get {
            return village || keep;
        }
    }

    public bool village = false;

    public bool chargeWorks = false;
    public bool keep = false;
    public bool castle = false;

    public bool navigableWaterway = false;
    public bool aquatic = false;
    public bool elevatedVision = false;

    public Color minimapColor;

    public UnitMod unitMod = new UnitMod();
}
