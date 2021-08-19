using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/VillageBuilding")]
public class VillageBuilding : GWScriptableObject
{
    [System.Serializable]
    public class UpgradeInfo
    {
        public EconomyBuilding upgradeRequirement;
        public VillageBuilding upgradeTo;
    }

    public List<UpgradeInfo> upgrades;

    public VillageBuilding GetUpgradedVersion(TeamInfo teamInfo)
    {
        foreach(UpgradeInfo info in upgrades) {
            if(teamInfo.buildingsCompleted.Contains(info.upgradeRequirement)) {
                return info.upgradeTo;
            }
        }

        return this;
    }

    public Sprite icon;
    public string description;
    public string rulesText;

    public int xpGainEndTurn = 0;

    public int goldCost = 5;
    public int timeCost = 1;

    public int goldIncome = 0;
    public int buildingIncome = 0;

    public int temporaryHitpointsEndOfTurn;

    public List<Equipment> equipmentAvailablePool;
    public int equipmentAvailableCount = -1;

    public List<int> equipmentAvailableByTier;

    public List<UnitStatus> giveStatusAfterRest;

    public List<Equipment> giveEquipmentOnEndTurn;

    public List<TerrainRules> mustBeAdjacentToTerrain;

    public bool mustBeAdjacentToOcean = false;
    public bool mustBeAdjacentToRiver = false;

    public bool acceleratesResting = false;

    public bool regenerateAtEndOfTurn = false;

    public bool allowsChangingSpells = false;

    public UnitMod unitMod;

    public bool LocationEligible(Loc loc)
    {
        string reason;
        return LocationEligible(loc, out reason);
    }

    public bool LocationEligible(Loc loc, out string reason)
    {
        if(mustBeAdjacentToOcean && GameController.instance.map.AdjacentToOcean(loc) == false) {
            reason = "Must be next to ocean";
            return false;
        }

        if(mustBeAdjacentToRiver) {
            bool nextToRiver = false;
            Tile t = GameController.instance.map.GetTile(loc);
            foreach(Tile adj in t.adjacentTiles) {
                if(adj != null && adj.freshwater) {
                    nextToRiver = true;
                    break;
                }
            }

            if(nextToRiver == false) {
                reason = "Must be next to a river";
                return false;
            }
        }

        if(mustBeAdjacentToTerrain != null && mustBeAdjacentToTerrain.Count > 0) {
            bool found = false;
            Tile t = GameController.instance.map.GetTile(loc);
            foreach(Tile adj in t.adjacentTiles) {
                if(adj != null && mustBeAdjacentToTerrain.Contains(adj.terrain.rules)) {
                    found = true;
                    break;
                }
            }

            if(found == false) {
                reason = "Must be next to " + mustBeAdjacentToTerrain[0].terrainName;
                if(mustBeAdjacentToTerrain.Count > 1) {
                    for(int i = 1; i < mustBeAdjacentToTerrain.Count; ++i) {
                        reason += " or " + mustBeAdjacentToTerrain[i].terrainName;
                    }
                }
                return false;
            }
        }

        reason = null;

        return true;
    }

    public void UnitEndTurn(Unit unit)
    {
        if(giveEquipmentOnEndTurn != null && giveEquipmentOnEndTurn.Count > 0) {
            List<Equipment> candidates = new List<Equipment>();
            foreach(Equipment equip in giveEquipmentOnEndTurn) {
                if(equip.EquippableForUnit(unit.unitInfo)) {
                    candidates.Add(equip);
                }
            }

            if(candidates.Count > 0) {
                Equipment equip = candidates[GameController.instance.rng.Range(0, candidates.Count)];
                unit.GiveUnitEquipment(equip);
            }
        }

        if(xpGainEndTurn != 0) {
            unit.unitInfo.GainExperience(xpGainEndTurn);
        }
    }


    public void UnitFinishResting(Unit unit)
    {
        if(giveStatusAfterRest != null && giveStatusAfterRest.Count > 0 && unit.unitInfo.blessed == false) {
            var status = giveStatusAfterRest[GameController.instance.rng.Range(0, giveStatusAfterRest.Count)];
            unit.ApplyStatus(status);
        }
    }

    public void UnitVisitingMarket(Unit unit)
    {
        if(unit.teamInfo.HasTemporaryMarket(unit.loc) == false) {

            List<Equipment> availableItems = new List<Equipment>();

            if(equipmentAvailablePool.Count > 0) {
                availableItems = new List<Equipment>(equipmentAvailablePool);

                if(equipmentAvailableCount >= 0 || equipmentAvailableCount < equipmentAvailablePool.Count) {
                    while(availableItems.Count > equipmentAvailableCount) {
                        availableItems.RemoveAt(GameController.instance.rng.Range(0, availableItems.Count));
                    }
                }
            }

            for(int tier = 0; tier < equipmentAvailableByTier.Count; ++tier) {
                int nitems = equipmentAvailableByTier[tier];
                if(nitems <= 0) {
                    continue;
                }

                TeamInfo.MarketInfo market = unit.teamInfo.GetMarketUnitHasAccessTo(unit);
                List<Equipment> candidates = new List<Equipment>();

                foreach(Equipment equip in Equipment.all) {
                    if(equip.tier != tier || market.equipment.Contains(equip)) {
                        continue;
                    }

                    candidates.Add(equip);
                }

                for(int i = 0; i < nitems && candidates.Count > 0; ++i) {
                    int index = GameController.instance.rng.Range(0, candidates.Count);
                    availableItems.Add(candidates[index]);
                    candidates.RemoveAt(index);
                }
            }

            if(availableItems.Count > 0) {
                TeamInfo.MarketInfo newMarket = new TeamInfo.MarketInfo() {
                    equipment = availableItems,
                    info = string.Format("From {0}", description),
                };

                unit.teamInfo.AddTemporaryMarket(unit.loc, newMarket);
            }
        }
    }
}
