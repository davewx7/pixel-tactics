using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[System.Serializable]
public class DungeonInfo
{
    public string guid;

    public bool playerEntered = false;

    public Loc entryLoc;

    //If valid, the player has to move here to have considered to have looted/cleared the dungeon.
    public Loc clearedLoc;

    //If valid, the player has to kill this unit to have looted/cleared the dungeon.
    public string rulerGuid;

    public string monsterDescription = "monsters";
    public string monsterTooltip = "";

    public List<Loc> interiorLocs;
}

[CreateAssetMenu(menuName = "Wesnoth/Boon/Underworld")]
public class BoonUnderworld : Boon
{
    [TextArea(3, 3)]
    public string questAcceptedText;

    [SerializeField]
    int _minRound = 0;

    [SerializeField]
    List<MapGenerator.TreasureCavernElement> _cavern = null;

    [SerializeField]
    Terrain _caveFloorTerrain = null;

    [SerializeField]
    Terrain _caveHillsTerrain = null;

    [SerializeField]
    Terrain _caveForestTerrain = null;

    [SerializeField]
    int _percentHills = 10;

    [SerializeField]
    int _percentForest = 10;



    [SerializeField]
    Terrain _caveWallTerrain = null;

    [SerializeField]
    List<TerrainRules> _caveEntryTerrainRules = null;

    [SerializeField]
    List<TerrainRules> _caveBeneathTerrainRules = null;


    static GameMap _map {
        get {
            return GameController.instance.map;
        }
    }

    static GameMap _underworldMap {
        get {
            return GameController.instance.underworldMap;
        }
    }

    static bool IsLocUsable(Loc loc)
    {
        if(_map.LocOnBoard(loc) == false) {
            return false;
        }

        Tile overworldTile = _map.GetTile(loc.toOverworld);
        if(overworldTile.underworldGate) {
            return false;
        }

        Tile t = _map.GetTile(loc.toUnderworld);
        return t.isvoid && t.gameObject.activeSelf == false && t.terrain.terrain != GameConfig.instance.caveWallTerrain;
    }

    static bool IsLocAndAdjacentUsable(Loc loc)
    {
        if(IsLocUsable(loc) == false) {
            return false;
        }

        Loc[] adj = Tile.AdjacentLocs(loc);
        foreach(Loc a in adj) {
            if(IsLocUsable(loc) == false) {
                return false;
            }
        }

        return true;
    }

    List<Loc> GenerateCavernArea(Unit unit)
    {
        return GenerateCavernArea(unit.loc);
    }

    public List<Loc> GenerateCavernArea(Loc loc, ConsistentRandom rng=null)
    {
        if(rng == null) {
            rng = new ConsistentRandom(loc.y*256 + loc.x);
        }

        Debug.Log("UNDERWORLDBOON Generate cavern...");
        List<Loc> result = new List<Loc>();
        List<Loc> live = new List<Loc>();
        live.Add(loc);

        int minDepth = 0;

        while(result.Count <= _cavern.Count) {

            if(result.Count < _cavern.Count && _cavern[result.Count].minDepth > minDepth) {
                minDepth = _cavern[result.Count].minDepth;
            }

            Loc currentLoc = live[live.Count-1];

            Loc[] adj = Tile.AdjacentLocs(currentLoc);
            int n = rng.Next(0, 6);
            Loc nextLoc = Loc.invalid;
            for(int i = 0; i != 6; ++i) {
                Loc a = adj[(n+i)%6];
                if(result.Contains(a) == false && IsLocAndAdjacentUsable(a) && Tile.DistanceBetween(a, loc) > minDepth) {
                    Tile t = _map.GetTile(a);
                    if(result.Count == 0 && _caveEntryTerrainRules.Contains(t.terrain.rules) == false) {
                        continue;
                    }

                    if(result.Count > 0 && _caveBeneathTerrainRules.Contains(t.terrain.rules) == false) {
                        continue;
                    }

                    nextLoc = a;
                    break;
                }
            }

            if(nextLoc.valid == false) {
                if(live.Count > 3) {
                    live.RemoveAt(live.Count-1);
                } else {
                    return null;
                }
            } else {
                Debug.Log("UNDERWORLD BOON NEXT LOC: " + nextLoc);
                result.Add(nextLoc);
                live.Add(nextLoc);
            }
        }
        return result;
    }

    public override bool IsEligible(Unit unit)
    {
        if(GameController.instance.gameState.GetLastBoonOfferRound(this) >= 0) {
            //This only gets offered once.
            return false;
        }

        if(GameController.instance.gameState.nround < _minRound) {
            return false;
        }

        //Must be no underworld gates within 6 tiles.
        foreach(Loc loc in Tile.GetTilesInRadius(unit.loc, 6)) {
            if(_map.LocOnBoard(loc) == false) {
                continue;
            }

            Tile tile = GameController.instance.map.GetTile(loc);
            if(tile.underworldGate) {
                return false;
            }
        }



        return GenerateCavernArea(unit) != null;
    }

    public void SpawnCavern(List<Loc> locs, MapGenerator.TreasureCavern treasureCavernInfo=null)
    {
        //If there is an enemy team in this cave, spawn them.
        TeamInfo underworldteam = null;
        int underworldTeamNumber = GameController.instance.gameState.numBarbarianTeam;

        if(treasureCavernInfo != null && treasureCavernInfo.underworldTeam != null) {
            Debug.Log("Add underworld team: " + GameController.instance.gameState.teams.Count);
            underworldTeamNumber = GameController.instance.gameState.teams.Count;
            underworldteam = new TeamInfo(treasureCavernInfo.underworldTeam);
            GameController.instance.gameState.teams.Add(underworldteam);
            GameController.instance.gameState.aiStates.Add(new AIState() {
                targetLoc = GameController.instance.gameState.teams[0].keepLoc,
                teamNumber = GameController.instance.aiStates.Count,
                gateToOverworld = locs[0].toOverworld,
            });

            if(GameController.instance.gameState.teams.Count != GameController.instance.gameState.aiStates.Count) {
                Debug.LogErrorFormat("AIState has bad index: {0}/{1}", GameController.instance.gameState.teams.Count, GameController.instance.gameState.aiStates.Count);
            }
        }

        //the point at which the player has to move to consider this cave looted.
        Loc lootPoint = Loc.invalid;

        Tile entryPoint = _map.GetTile(locs[0].toOverworld);
        entryPoint.underworldGate = true;

        UnitInfo rulerInfo = null;

        //forbid the barbarians from using this gate.
        GameController.instance.gameState.aiStates[GameController.instance.gameState.numBarbarianTeam].forbiddenLocs.Add(locs[0].toOverworld);

        List<Tile> dirtyTiles = new List<Tile>();

        //Calculate all cave locs, not just ones on the main path.
        List<Loc> caveLocs = new List<Loc>();
        for(int i = 1; i < locs.Count; ++i) {
            var element = _cavern[i-1];
            foreach(Loc loc in Tile.GetTilesInRadius(locs[i], element.maxRadius)) {
                if(_underworldMap.LocOnBoard(loc) && caveLocs.Contains(loc) == false && loc.toUnderworld != locs[0].toUnderworld && IsLocAndAdjacentUsable(loc)) {
                    int dist = Tile.DistanceBetween(loc, locs[i]);
                    float chance = 1f;
                    if(dist > element.minRadius && element.maxRadius > element.minRadius) {
                        chance -= (dist - element.minRadius) / (float)(element.maxRadius - element.minRadius);
                    }

                    if(chance >= 1f || (GameController.instance.rng.Next()%100)*0.01f < chance) {
                        caveLocs.Add(loc.toUnderworld);
                    }
                }
            }
        }

        foreach(Loc loc in caveLocs) {
            Tile tile = _underworldMap.GetTile(loc);
            tile.isvoid = false;
            tile.gameObject.SetActive(true);


            var terrain = _caveFloorTerrain;
            if(locs.Contains(loc) == false) {
                int nrng = GameController.instance.rng.Next();
                if(nrng%100 < _percentForest) {
                    terrain = _caveForestTerrain;
                } else if(nrng%100 < _percentForest + _percentHills) {
                    terrain = _caveHillsTerrain;
                }
            }

            tile.terrain = new TerrainInfo(terrain);

            dirtyTiles.Add(tile);

            foreach(Loc adj in Tile.AdjacentLocs(loc)) {
                if(locs.Contains(adj) || adj.toUnderworld == locs[0].toUnderworld) {
                    continue;
                }

                Tile adjTile = _underworldMap.GetTile(adj);
                if(adjTile != null && adjTile.isvoid) {
                    adjTile.isvoid = false;
                    adjTile.gameObject.SetActive(true);
                    adjTile.terrain = new TerrainInfo(_caveWallTerrain);

                    dirtyTiles.Add(adjTile);
                }
            }
        }

        Loc keepLoc = Loc.invalid;

        for(int i = 1; i < locs.Count; ++i) {
            Loc loc = locs[i];
            Tile tile = _underworldMap.GetTile(loc);
            var element = _cavern[i-1];
            if(element.loot != null) {
                tile.AddLoot(element.loot);

                if(rulerInfo == null) {
                    lootPoint = loc;
                }
            }

            if(element.unitType != null || element.spawnKeep) {
                UnitType unitType = element.unitType;
                if(unitType == null) {
                    unitType = treasureCavernInfo.underworldTeam.rulerType;
                }
                UnitInfo unitInfo = unitType.createUnit();
                if(element.spawnKeep) {
                    unitInfo.ruler = true;
                    rulerInfo = unitInfo;
                    lootPoint = Loc.invalid;
                }
                unitInfo.nteam = underworldTeamNumber;
                unitInfo.loc = loc.toUnderworld;
                GameController.instance.ExecuteSpawnUnit(unitInfo);
            }

            if(element.terrainOverride != null) {
                tile.terrain = new TerrainInfo(element.terrainOverride);
            }

            if(element.spawnKeep) {
                keepLoc = loc;
            }
        }

        if(keepLoc.valid && treasureCavernInfo != null) {
            Tile keepTile = GameController.instance.map.GetTile(keepLoc.toUnderworld);
            keepTile.terrain = new TerrainInfo(treasureCavernInfo.keepTerrain);

            int ncount = 0;
            foreach(Tile adj in keepTile.adjacentTiles) {
                if(adj == null || adj.isvoid || adj.unit != null || adj.loot != null) {
                    continue;
                }

                adj.terrain = new TerrainInfo(treasureCavernInfo.castleTerrain);

                if(++ncount >= 2) {
                    break;
                }
            }
        }

        if(treasureCavernInfo != null && treasureCavernInfo.numVillages > 0) {
            List<Loc> possibleLocs = new List<Loc>();
            foreach(Loc loc in caveLocs) {
                if(Tile.DistanceBetween(loc, locs[0]) <= 1) {
                    continue;
                }

                Tile t = GameController.instance.map.GetTile(loc.toUnderworld);
                if(t.unit == null && t.loot == null && t.isvoid == false && t.terrain.rules.castle == false) {
                    possibleLocs.Add(loc);
                }
            }

            List<Loc> wantToBeFarFrom = new List<Loc>();
            wantToBeFarFrom.Add(locs[0]);

            for(int i = 0; i < treasureCavernInfo.numVillages && possibleLocs.Count > 0; ++i) {
                float bestScore = -1f;
                Loc bestLoc = Loc.invalid;
                foreach(Loc loc in possibleLocs) {
                    if(bestLoc.valid && GameController.instance.rng.Next()%2 == 0) {
                        continue;
                    }

                    float score = 100f;
                    foreach(Loc existing in wantToBeFarFrom) {
                        int dist = Tile.DistanceBetween(existing, loc);
                        if(dist < score) {
                            score = dist;
                        }
                    }

                    if(score > bestScore) {
                        bestLoc = loc;
                        bestScore = score;
                    }
                }

                Assert.IsTrue(bestLoc.valid);
                
                possibleLocs.Remove(bestLoc);
                wantToBeFarFrom.Add(bestLoc);

                Tile tile = GameController.instance.map.GetTile(bestLoc.toUnderworld);
                tile.terrain = new TerrainInfo(treasureCavernInfo.villageTerrain);

                GameController.instance.ForceCaptureLoc(tile.loc, underworldTeamNumber);
            }
        }

        if(rulerInfo != null) {
            lootPoint = Loc.invalid;
        }

        foreach(Tile t in dirtyTiles) {
            t.SetDirty();
        }

        GameController.instance.gameState.dungeonInfo.Add(new DungeonInfo() {
            guid = System.Guid.NewGuid().ToString(),
            entryLoc = locs[0],
            clearedLoc = lootPoint,
            rulerGuid = rulerInfo != null ? rulerInfo.guid : null,
            monsterDescription = underworldteam != null ? underworldteam.team.factionDescription : "monsters",
            monsterTooltip = underworldteam != null ? underworldteam.team.factionTooltipDescription : "",
            interiorLocs = caveLocs,
        });
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        List<Loc> locs = GenerateCavernArea(unit);
        SpawnCavern(locs);

        _underworldMap.SetupEdges();
        _map.SetupEdges();

        GameController.instance.ShowDialogMessage("A Dark Entryway", questAcceptedText, avatarSprite);
    }
}
