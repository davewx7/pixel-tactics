using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocText : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _text = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Unit unitDisplayed = GameController.instance.unitDisplayed;

        if(Tile.mouseoverTile == null || Tile.mouseoverTile.shrouded) {
            _text.text = "";
        } else {

            VillageBuilding building = GameController.instance.gameState.GetVillageBuilding(Tile.mouseoverTile.loc);

            int moveCost = 1;
            bool advantage = false, disadvantage = false;
            int evasion = 0;
            int armor = 0, resistance = 0;

            if(unitDisplayed == null) {
                moveCost = Tile.mouseoverTile.terrain.rules.moveCost;

            } else {
                advantage = unitDisplayed.unitInfo.AdvantageInTerrain(Tile.mouseoverTile.terrain.rules);
                disadvantage = unitDisplayed.unitInfo.DisadvantageInTerrain(Tile.mouseoverTile.terrain.rules);

                moveCost = unitDisplayed.unitInfo.MoveCost(Tile.mouseoverTile);
            }

            UnitMod unitMod = Tile.mouseoverTile.terrain.rules.unitMod;

            if(unitDisplayed != null) {
                unitMod = unitDisplayed.unitInfo.GetModForTerrain(Tile.mouseoverTile.terrain.rules);
            }

            armor = unitMod.armor;
            resistance = unitMod.resistance;
            evasion = unitMod.evasion;

            if(building != null && building.unitMod != null && building.unitMod.useMod) {
                armor += building.unitMod.armor;
                resistance += building.unitMod.resistance;
                evasion += building.unitMod.evasion;
            }

            string abilities = "";
            foreach(var ability in unitMod.abilities) {
                abilities += "<color=#88CC88>" + ability.description + "</color> ";
            }

            string villageInfo = "";
            if(Tile.mouseoverTile.terrain.villageInfo != null) {
                villageInfo = string.Format(" ({0})", Tile.mouseoverTile.terrain.villageInfo.villageDescription);
            }

            string details = "";
            if(advantage) {
                details += "<color=#88CC88>Advantage</color> ";
            } else if(disadvantage) {
                details += "<color=#CC8888>Disadvantage</color> ";
            }

            if(moveCost > 3) {
                details += "<color=#CC8888>Impassable</color> ";
                evasion = 0;
                armor = 0;
                resistance = 0;
            } else {
                details += string.Format("MV {0} ", moveCost);
            }

            if(evasion < 0) {
                details += string.Format("<color=#CC8888>EV {0}%</color> ", evasion);
            } else if(evasion > 0) {
                details += string.Format("<color=#88CC88>EV +{0}%</color> ", evasion);
            }

            if(armor > 0) {
                details += string.Format("<color=#88CC88>ARM +{0}</color> ", armor);
            }

            if(resistance > 0) {
                details += string.Format("<color=#88CC88>RES +{0}</color> ", resistance);
            }

            if(Tile.mouseoverTile.terrain.rules.village) {
                int nowner = GameController.instance.gameState.GetOwnerOfLoc(Tile.mouseoverTile.loc);
                if(nowner < 0) {
                    nowner = 0;
                }
                details += string.Format("<color=#88CC88>heals {0}</color> ", GameController.instance.teams[nowner].villageHealAmount);
            }

            _text.text = string.Format("{0}{1} {2}{3}({4},{5})", Tile.mouseoverTile.terrain.rules.terrainName, villageInfo, details, abilities, Tile.mouseoverTile.loc.x, Tile.mouseoverTile.loc.y);
        }

    }
}
